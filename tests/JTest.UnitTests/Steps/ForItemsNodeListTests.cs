using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.UnitTests.TestHelpers;
using System.Text.Json;

namespace JTest.UnitTests.Steps;

/// <summary>
/// A `for` step reads a `{{...}}` `items` expression as a JSONPath node list: a filter iterates once
/// per match whether it matches zero, one or many nodes. (Elsewhere a single match still resolves to
/// the value itself, which existing templates rely on.)
/// </summary>
public class ForItemsNodeListTests
{
    private static TestExecutionContext ContextWithItems()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?>
        {
            ["body"] = new Dictionary<string, object?>
            {
                ["items"] = new object?[]
                {
                    new Dictionary<string, object?> { ["id"] = "a", ["artifactVersion"] = "1.0.0" },
                    new Dictionary<string, object?> { ["id"] = "b", ["artifactVersion"] = "2.0.0" },
                    new Dictionary<string, object?> { ["id"] = "c", ["artifactVersion"] = "2.0.0" }
                }
            }
        };
        return context;
    }

    private static Task<StepProcessedResult> RunFor(string itemsExpression, TestExecutionContext context)
    {
        var json = """{ "type": "for", "items": """ + JsonSerializer.Serialize(itemsExpression) + """
        , "steps": [ { "type": "wait", "ms": 1, "save": { "$.seen[]": "{{ $.item.id }}" } } ] }
        """;
        var step = JsonSerializer.Deserialize<IStep>(json, JsonSerializerHelper.Options)!;
        return StepProcessor.Default.ProcessStep(step, context, default);
    }

    private static object?[] Seen(TestExecutionContext context) =>
        context.Variables.TryGetValue("seen", out var seen) ? (object?[])seen! : [];

    [Fact]
    public async Task FilterWithTwoMatches_IteratesTwice()
    {
        var context = ContextWithItems();

        var result = await RunFor("{{ $.this.body.items[?(@.artifactVersion == '2.0.0')] }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(new object?[] { "b", "c" }, Seen(context));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task FilterWithOneMatch_IteratesOnceOverTheMatchedObject()
    {
        var context = ContextWithItems();

        var result = await RunFor("{{ $.this.body.items[?(@.artifactVersion == '1.0.0')] }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(new object?[] { "a" }, Seen(context));
        Assert.Equal(1, ((Dictionary<string, object?>)result.Data!)["completedIterationCount"]);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task FilterWithNoMatch_RunsZeroIterations_AndWarns()
    {
        var context = ContextWithItems();

        var result = await RunFor("{{ $.this.body.items[?(@.artifactVersion == '9.9.9')] }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(Seen(context));
        Assert.Equal(0, ((Dictionary<string, object?>)result.Data!)["completedIterationCount"]);
        var warning = Assert.Single(result.Warnings);
        Assert.StartsWith("items: ", warning);
        Assert.Contains("matched nothing", warning);
        Assert.Contains("zero iterations", warning);
    }

    [Fact]
    public async Task MissingPath_RunsZeroIterations_AndWarns()
    {
        var context = ContextWithItems();

        var result = await RunFor("{{ $.unknownToken }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(Seen(context));
        Assert.Contains("$.unknownToken", Assert.Single(result.Warnings));
    }

    [Fact]
    public async Task PathToAnArray_StillIteratesItsElements()
    {
        var context = ContextWithItems();

        var result = await RunFor("{{ $.this.body.items }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(new object?[] { "a", "b", "c" }, Seen(context));
    }

    [Fact]
    public async Task UnresolvedTokenInsideLargerString_StillFails()
    {
        var context = ContextWithItems();

        await Assert.ThrowsAsync<Core.Exceptions.StepConfigurationValidationException>(
            () => RunFor("prefix-{{ $.unknownToken }}", context));
    }

    [Theory]
    [InlineData("2", new[] { "b", "c" })]
    [InlineData("1", new[] { "a" })]
    [InlineData("9", new string[0])]
    public async Task FilterWithNestedToken_HasTheSameNodeListSemantics(string major, string[] expected)
    {
        var context = ContextWithItems();
        context.Variables["major"] = major;

        var result = await RunFor("{{ $.this.body.items[?(@.artifactVersion == '{{ $.major }}.0.0')] }}", context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, Seen(context).Cast<string>().ToArray());
        Assert.Equal(expected.Length == 0 ? 1 : 0, result.Warnings.Count);
    }

    [Fact]
    public async Task FilterWithNestedToken_WhenInnerTokenMatchesNothing_Fails()
    {
        var context = ContextWithItems();

        await Assert.ThrowsAsync<Core.Exceptions.StepConfigurationValidationException>(
            () => RunFor("{{ $.this.body.items[?(@.artifactVersion == '{{ $.missing }}')] }}", context));
    }
}
