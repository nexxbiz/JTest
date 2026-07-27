using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;

namespace JTest.Engine.Tests.Contexts;

[TestClass]
public sealed class ExecutionFrameTests
{
    private static ExecutionFrame CaseFrame() =>
        ExecutionFrame.CreateCase(
            new JsonObject { ["baseUrl"] = "https://api.test" },
            new JsonObject { ["token"] = "g" },
            new JsonObject { ["sku"] = "widget" });

    [TestMethod]
    public void LoopFramesShareTheCaseScratchScope()
    {
        var parent = CaseFrame();
        var loop = ExecutionFrame.CreateLoop(parent, new Dictionary<string, JsonNode?> { ["item"] = "a" });

        Assert.IsTrue(ScopeWriter.TryApply(loop, "$.ctx.savedInLoop", JsonValue.Create(42)));

        Assert.AreEqual(42, parent.Ctx["savedInLoop"]!.GetValue<int>());
    }

    [TestMethod]
    public void LoopBindingsResolveThroughNestedFrames()
    {
        var parent = CaseFrame();
        var outer = ExecutionFrame.CreateLoop(parent, new Dictionary<string, JsonNode?> { ["item"] = "outer", ["index"] = 0 });
        var inner = ExecutionFrame.CreateLoop(outer, new Dictionary<string, JsonNode?> { ["inner"] = "x" });

        var fromInner = ExpressionResolver.ResolveString("{{$.item}}/{{$.inner}}", inner, "t");

        Assert.IsTrue(fromInner.Success);
        Assert.AreEqual("outer/x", fromInner.Value!.GetValue<string>());
    }

    [TestMethod]
    public void StepIdsResolveUpwardThroughLoopFrames()
    {
        var parent = CaseFrame();
        parent.SetStepResult("login", new JsonObject { ["token"] = "abc" });
        var loop = ExecutionFrame.CreateLoop(parent, new Dictionary<string, JsonNode?> { ["item"] = 1 });

        var result = ExpressionResolver.ResolveString("{{$.login.token}}", loop, "t");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("abc", result.Value!.GetValue<string>());
    }

    [TestMethod]
    public void TemplateFramesIsolateCallerNamesButShareReadScopes()
    {
        var caller = CaseFrame();
        caller.SetStepResult("login", new JsonObject { ["token"] = "abc" });
        caller.Ctx["callerLocal"] = 1;

        var template = ExecutionFrame.CreateTemplate(
            caller, new Dictionary<string, JsonNode?> { ["user"] = "ci" });

        Assert.IsTrue(ExpressionResolver.ResolveString("{{$.env.baseUrl}}", template, "t").Success);
        Assert.IsTrue(ExpressionResolver.ResolveString("{{$.globals.token}}", template, "t").Success);
        Assert.IsTrue(ExpressionResolver.ResolveString("{{$.case.sku}}", template, "t").Success);
        Assert.AreEqual("ci", ExpressionResolver.ResolveString("{{$.user}}", template, "t").Value!.GetValue<string>());

        var callerStep = ExpressionResolver.ResolveString("{{$.login.token}}", template, "t");
        Assert.IsFalse(callerStep.Success);
        Assert.AreEqual(RuntimeDiagnosticCodes.UnresolvableExpression, callerStep.Diagnostic!.Code);

        var callerCtx = ExpressionResolver.ResolveString("{{$.ctx.callerLocal}}", template, "t");
        Assert.IsFalse(callerCtx.Success);
    }

    [TestMethod]
    public void SaveCreatesNestedObjectsOnDemand()
    {
        var frame = CaseFrame();

        Assert.IsTrue(ScopeWriter.TryApply(frame, "$.ctx.order.detail.id", JsonValue.Create("o-1")));
        Assert.IsTrue(ScopeWriter.TryApply(frame, "$.globals.session", JsonValue.Create("s-1")));

        Assert.AreEqual("o-1", frame.Ctx["order"]!["detail"]!["id"]!.GetValue<string>());
        Assert.AreEqual("s-1", frame.Globals["session"]!.GetValue<string>());
    }

    [TestMethod]
    public void SaveThroughNonObjectFailsClosed()
    {
        var frame = CaseFrame();
        frame.Ctx["scalar"] = 5;

        Assert.IsFalse(ScopeWriter.TryApply(frame, "$.ctx.scalar.child", JsonValue.Create(1)));
    }
}
