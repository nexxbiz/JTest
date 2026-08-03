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
}
