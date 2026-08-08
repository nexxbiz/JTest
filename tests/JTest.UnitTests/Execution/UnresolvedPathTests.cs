using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Utilities;
using Xunit;

namespace JTest.UnitTests.Execution;

public class UnresolvedPathTests
{
    [Fact]
    public void ResolvedPath_ReturnsValue()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        var result = VariableInterpolator.TryResolveJsonPath("$.this.status", context);

        Assert.Equal(VariableInterpolator.PathResolution.Resolved, result.Status);
        Assert.Equal(200, Convert.ToInt32(result.Value));
    }

    [Fact]
    public void UnresolvedPath_IsMatchedNothing_NotSilentNull()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        // A casing/typo mismatch ('statusCode' vs 'status') matches nothing.
        var result = VariableInterpolator.TryResolveJsonPath("$.this.statusCode", context);

        Assert.Equal(VariableInterpolator.PathResolution.MatchedNothing, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public void MatchedNothing_IsDistinctFromResolved()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        var resolved = VariableInterpolator.TryResolveJsonPath("$.this.status", context).Status;
        var missing = VariableInterpolator.TryResolveJsonPath("$.this.nope", context).Status;

        Assert.NotEqual(resolved, missing);
    }

    // The requirement is only met where authors actually meet it — inside an assertion and a save,
    // not just in the resolver helper. These cover that path.

    [Fact]
    public void Interpolator_ReportsUnresolvedPath()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        VariableInterpolator.ResolveVariableTokens("{{$.this.nope}}", context, out var unresolved);

        Assert.Equal(["$.this.nope"], unresolved);
    }

    [Fact]
    public void Interpolator_ResolvedPath_ReportsNoUnresolved()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        VariableInterpolator.ResolveVariableTokens("{{$.this.status}}", context, out var unresolved);

        Assert.Empty(unresolved);
    }

    [Fact]
    public void Assertion_WithUnresolvedPath_FailsWithPathDiagnostic_NotBlankComparison()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?>
        {
            ["body"] = new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 }
        };

        // '.length' is a JavaScript-ism: RFC 9535 JSONPath has no such property, so it matches
        // nothing and the comparison would otherwise fail as a blank that looks like a data problem.
        var result = new EqualsAssertion("{{$.this.body.length}}", 2).Execute(context);

        Assert.False(result.Success);
        Assert.Contains("$.this.body.length", result.ErrorMessage);
        Assert.Contains("matched nothing", result.ErrorMessage);
        Assert.Contains("length' assertion operator", result.ErrorMessage);
        Assert.Equal(["$.this.body.length"], result.UnresolvedPaths);
    }

    [Fact]
    public void Assertion_WithResolvedPath_RecordsNoUnresolvedPaths()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        var result = new EqualsAssertion("{{$.this.status}}", 200).Execute(context);

        Assert.True(result.Success);
        Assert.Empty(result.UnresolvedPaths);
    }

    [Fact]
    public void NotExistsAssertion_WithUnresolvedPath_StillPasses()
    {
        var context = new TestExecutionContext();
        context.Variables["this"] = new Dictionary<string, object?> { ["status"] = 200 };

        // For existence operators a path that matches nothing is the answer, not a broken expression.
        var result = new NotExistsAssertion("{{$.this.nope}}").Execute(context);

        Assert.True(result.Success);
    }

    [Fact]
    public void UnresolvedPath_HintsAtCasing_WhenNotAKnownJavaScriptism()
    {
        var message = VariableInterpolator.DescribeUnresolvedPath("$.this.headers['Content-Type']");

        Assert.Contains("matched nothing", message);
        Assert.Contains("case-sensitive", message);
    }
}
