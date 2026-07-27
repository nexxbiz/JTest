using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;

namespace JTest.Engine.Tests.Expressions;

[TestClass]
public sealed class ExpressionResolverTests
{
    private static ExecutionFrame Frame()
    {
        var env = new JsonObject { ["baseUrl"] = "https://api.test", ["port"] = 8080, ["ratio"] = 1234.5 };
        var globals = new JsonObject { ["attempt"] = 3 };
        var caseValues = new JsonObject { ["sku"] = "widget" };
        return ExecutionFrame.CreateCase(env, globals, caseValues);
    }

    [TestMethod]
    public void SingleTokenYieldsTypedValue()
    {
        var result = ExpressionResolver.ResolveString("{{$.env.port}}", Frame(), "t");
        Assert.IsTrue(result.Success);
        Assert.AreEqual(8080, result.Value!.GetValue<int>());
    }

    [TestMethod]
    public void EmbeddedTokensStringifyPositionExactly()
    {
        var frame = Frame();
        frame.Ctx["a"] = "{{$.ctx.b}}";
        frame.Ctx["b"] = "X";

        var result = ExpressionResolver.ResolveString("{{$.ctx.a}}-{{$.ctx.b}}", frame, "t");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("{{$.ctx.b}}-X", result.Value!.GetValue<string>());
    }

    [TestMethod]
    public void ResolvedValuesAreNeverReinterpreted()
    {
        var frame = Frame();
        frame.Ctx["hostile"] = "{{$.env.baseUrl}}";

        var result = ExpressionResolver.ResolveString("{{$.ctx.hostile}}", frame, "t");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("{{$.env.baseUrl}}", result.Value!.GetValue<string>());
    }

    [TestMethod]
    public void UnresolvablePathFailsClosed()
    {
        var result = ExpressionResolver.ResolveString("{{$.env.missing}}", Frame(), "t");
        Assert.IsFalse(result.Success);
        Assert.AreEqual(RuntimeDiagnosticCodes.UnresolvableExpression, result.Diagnostic!.Code);
    }

    [TestMethod]
    public void UnknownScopeFailsClosed()
    {
        var result = ExpressionResolver.ResolveString("{{$.nowhere.x}}", Frame(), "t");
        Assert.IsFalse(result.Success);
        Assert.AreEqual(RuntimeDiagnosticCodes.UnresolvableExpression, result.Diagnostic!.Code);
    }

    [TestMethod]
    public void StringificationUsesInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var result = ExpressionResolver.ResolveString("r={{$.env.ratio}}", Frame(), "t");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("r=1234.5", result.Value!.GetValue<string>());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void ObjectTemplatesResolveRecursively()
    {
        using var template = JsonDocument.Parse(
            """{ "sku": "{{$.case.sku}}", "attempt": "{{$.globals.attempt}}", "list": ["{{$.env.port}}", 7] }""");

        var result = ExpressionResolver.ResolveValue(template.RootElement, Frame(), "t");

        Assert.IsTrue(result.Success);
        var resolved = result.Value!.AsObject();
        Assert.AreEqual("widget", resolved["sku"]!.GetValue<string>());
        Assert.AreEqual(3, resolved["attempt"]!.GetValue<int>());
        Assert.AreEqual(8080, resolved["list"]![0]!.GetValue<int>());
        Assert.AreEqual(7, resolved["list"]![1]!.GetValue<int>());
    }

    [TestMethod]
    public void JsonPathFiltersWorkAgainstScopeRoots()
    {
        var frame = Frame();
        frame.SetStepResult("fetch", new JsonObject
        {
            ["response"] = new JsonObject
            {
                ["body"] = new JsonArray(
                    new JsonObject { ["id"] = 1, ["state"] = "open" },
                    new JsonObject { ["id"] = 2, ["state"] = "closed" }),
            },
        });

        var result = ExpressionResolver.ResolveString(
            "{{$.fetch.response.body[?(@.state == 'closed')].id}}", frame, "t");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Value!.GetValue<int>());
    }

    [TestMethod]
    public void ThisScopeExposesPreviousStepResult()
    {
        var frame = Frame();
        frame.SetStepResult(null, new JsonObject { ["status"] = 201 });

        var result = ExpressionResolver.ResolveString("{{$.this.status}}", frame, "t");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(201, result.Value!.GetValue<int>());
    }
}
