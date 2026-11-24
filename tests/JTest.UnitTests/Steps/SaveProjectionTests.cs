using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.UnitTests.TestHelpers;
using NSubstitute;
using System.Text.Json;

namespace JTest.UnitTests.Steps;

/// <summary>
/// Projecting one API shape into another inside a loop or template: structured `save` values are
/// resolved when saved (not re-resolved later against the last loop iteration), and a target ending
/// in `[]` appends to an array.
/// </summary>
public class SaveProjectionTests
{
    private const string ProjectionTemplates = """
    {
        "version": "1.0",
        "components": {
            "templates": [
                {
                    "name": "project-instances",
                    "params": {
                        "items": { "type": "array", "required": true },
                        "map": { "type": "object", "default": { "Completed": "Finished", "Suspended": "Running" } }
                    },
                    "steps": [
                        { "type": "wait", "ms": 1, "save": { "$.out": [] } },
                        {
                            "type": "for",
                            "items": "{{ $.items }}",
                            "steps": [
                                {
                                    "type": "wait", "ms": 1,
                                    "save": { "$.out[]": { "id": "{{ $.item.id }}", "status": "{{ $.map['{{ $.item.s }}'] }}" } }
                                }
                            ]
                        }
                    ],
                    "output": { "out": "{{ $.out }}" }
                }
            ]
        }
    }
    """;

    private static async Task<StepProcessedResult> Run(string stepJson, TestExecutionContext context, string? templatesJson = null)
    {
        var templateContext = Substitute.For<ITemplateContext>();
        if (templatesJson is not null)
        {
            var collection = JsonSerializer.Deserialize<TemplateCollection>(templatesJson, JsonSerializerHelper.Options)!;
            templateContext.GetTemplate(Arg.Any<string>())
                .Returns(call => collection.Components!.Templates.Single(t => t.Name == call.Arg<string>()));
        }

        var options = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var step = JsonSerializer.Deserialize<IStep>(stepJson, options)!;
        return await StepProcessor.Default.ProcessStep(step, context, default);
    }

    private static object? Resolve(string expression, TestExecutionContext context) =>
        VariableInterpolator.ResolveVariableTokens(expression, context);

    [Fact]
    public async Task StructuredSaveValue_InsideLoop_CapturesEachIterationsValues()
    {
        var context = new TestExecutionContext();
        context.Variables["map"] = new Dictionary<string, object?> { ["Completed"] = "Finished", ["Suspended"] = "Running" };

        var result = await Run("""
        {
            "type": "for",
            "items": [ { "id": "a", "s": "Completed" }, { "id": "b", "s": "Suspended" } ],
            "steps": [
                {
                    "type": "wait", "ms": 1,
                    "save": { "$.byId.{{ $.item.id }}": { "id": "{{ $.item.id }}", "status": "{{ $.map['{{ $.item.s }}'] }}" } }
                }
            ]
        }
        """, context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("a", Resolve("{{ $.byId.a.id }}", context));
        Assert.Equal("Finished", Resolve("{{ $.byId.a.status }}", context));
        Assert.Equal("b", Resolve("{{ $.byId.b.id }}", context));
        Assert.Equal("Running", Resolve("{{ $.byId.b.status }}", context));
    }

    [Fact]
    public async Task StructuredSaveValue_WholeTokenKeepsType_EmbeddedTokenInterpolates()
    {
        var context = new TestExecutionContext();
        context.Variables["src"] = new Dictionary<string, object?> { ["n"] = 42, ["tags"] = new object?[] { "x", "y" }, ["name"] = "bob" };

        await Run("""
        { "type": "wait", "ms": 1, "save": { "$.ctx.snap": { "n": "{{ $.src.n }}", "tags": ["{{ $.src.tags }}", 7], "greeting": "hi {{ $.src.name }}!" } } }
        """, context);

        var snap = Assert.IsType<Dictionary<string, object?>>(((Dictionary<string, object?>)context.Variables["ctx"]!)["snap"]);
        Assert.Equal(42, snap["n"]);
        Assert.Equal("hi bob!", snap["greeting"]);
        var tags = Assert.IsType<object?[]>(snap["tags"]);
        Assert.Equal(7d, Convert.ToDouble(tags[1])); // numeric literals in `save` keep their pre-existing (double) typing
        Assert.Equal(2, Assert.IsAssignableFrom<System.Collections.IEnumerable>(tags[0]).Cast<object?>().Count());
    }

    [Fact]
    public async Task StructuredSaveValue_UnresolvedNestedPath_WarnsLikeTopLevelValues()
    {
        var context = new TestExecutionContext();

        var result = await Run("""
        { "type": "wait", "ms": 1, "save": { "$.ctx.snap": { "inner": { "missing": "{{ $.nope.value }}" } } } }
        """, context);

        var warning = Assert.Single(result.Warnings);
        Assert.Contains("save '$.ctx.snap'", warning);
        Assert.Contains("$.nope.value", warning);
    }

    [Fact]
    public async Task AppendTarget_CreatesArray_ThenAppendsAtTopLevelAndInScope()
    {
        var context = new TestExecutionContext();

        await Run("""{ "type": "wait", "ms": 1, "save": { "$.instances[]": { "id": "a" }, "$.ctx.list[]": 1 } }""", context);
        await Run("""{ "type": "wait", "ms": 1, "save": { "$.instances[]": { "id": "b" }, "$.ctx.list[]": 2 } }""", context);

        var instances = Assert.IsType<object?[]>(context.Variables["instances"]);
        Assert.Equal(2, instances.Length);
        Assert.Equal("b", Resolve("{{ $.instances[1].id }}", context));
        var list = Assert.IsType<object?[]>(((Dictionary<string, object?>)context.Variables["ctx"]!)["list"]);
        Assert.Equal(new[] { 1d, 2d }, list.Select(Convert.ToDouble));
    }

    [Fact]
    public async Task EmptyArraySave_InitialisesArray_AppendsFollow()
    {
        var context = new TestExecutionContext();

        await Run("""{ "type": "wait", "ms": 1, "save": { "$.out": [] } }""", context);
        Assert.Empty(Assert.IsType<object?[]>(context.Variables["out"]));

        await Run("""{ "type": "wait", "ms": 1, "save": { "$.out[]": "x" } }""", context);
        Assert.Equal(new object?[] { "x" }, context.Variables["out"]);
    }

    [Fact]
    public async Task AppendTarget_ResolvesTokensInTargetPath()
    {
        var context = new TestExecutionContext();
        context.Variables["which"] = "picked";

        await Run("""{ "type": "wait", "ms": 1, "save": { "$.ctx.{{ $.which }}[]": "v" } }""", context);

        Assert.Equal(new object?[] { "v" }, ((Dictionary<string, object?>)context.Variables["ctx"]!)["picked"]);
    }

    [Fact]
    public async Task AppendTarget_OnNonArray_FailsNamingThePath()
    {
        var context = new TestExecutionContext();
        context.Variables["ctx"] = new Dictionary<string, object?> { ["name"] = "scalar" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Run("""{ "type": "wait", "ms": 1, "save": { "$.ctx.name[]": "v" } }""", context));

        Assert.Contains("Cannot append to '$.ctx.name'", ex.Message);
        Assert.Contains("not an array", ex.Message);
    }

    [Fact]
    public async Task NameKeyedMap_BuiltFromList_InsideLoop()
    {
        var context = new TestExecutionContext();

        var result = await Run("""
        {
            "type": "for",
            "items": [ { "name": "Text", "value": "hello" }, { "name": "Count", "value": 3 } ],
            "steps": [ { "type": "wait", "ms": 1, "save": { "$.state.{{ $.item.name }}": "{{ $.item.value }}" } } ]
        }
        """, context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("hello", Resolve("{{ $.state.Text }}", context));
        Assert.Equal(3, Resolve("{{ $.state.Count }}", context));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    [InlineData(0)]
    public async Task Template_LoopAppendsProjectedItems_AndOutputsTheList(int itemCount)
    {
        var allItems = new object?[]
        {
            new Dictionary<string, object?> { ["id"] = "a", ["s"] = "Completed" },
            new Dictionary<string, object?> { ["id"] = "b", ["s"] = "Suspended" }
        };
        var context = new TestExecutionContext();
        context.Variables["globals"] = new Dictionary<string, object?> { ["items"] = allItems.Take(itemCount).ToArray() };

        var result = await Run("""
        { "type": "use", "template": "project-instances", "with": { "items": "{{ $.globals.items }}" },
          "save": { "$.globals.out": "{{ $.this.out }}" } }
        """, context, ProjectionTemplates);

        Assert.True(result.Success, result.ErrorMessage);
        var output = Assert.IsAssignableFrom<System.Collections.IEnumerable>(Resolve("{{ $.globals.out }}", context))
            .Cast<object?>().ToList();
        Assert.Equal(itemCount, output.Count);
        if (itemCount >= 1)
        {
            Assert.Equal("a", Resolve("{{ $.globals.out[0].id }}", context));
            Assert.Equal("Finished", Resolve("{{ $.globals.out[0].status }}", context));
        }
        if (itemCount == 2)
        {
            Assert.Equal("b", Resolve("{{ $.globals.out[1].id }}", context));
            Assert.Equal("Running", Resolve("{{ $.globals.out[1].status }}", context));
        }
    }
}
