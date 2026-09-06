using System.Collections;
using JTest.Core.Execution;
using JTest.Core.Utilities;
using Xunit;

namespace JTest.UnitTests.Utilities;

/// <summary>
/// Guarantees JSONPath filter + multi-match resolution (used by save/assert/interpolation).
/// The pinned evaluator is JsonPath.Net (RFC 9535), whose filter selector is `?@.expr`.
/// </summary>
public class JsonPathFilterTests
{
    private static TestExecutionContext ContextWithItems()
    {
        var context = new TestExecutionContext();
        context.Variables["items"] = new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = 1, ["active"] = true },
            new Dictionary<string, object?> { ["id"] = 2, ["active"] = false },
            new Dictionary<string, object?> { ["id"] = 3, ["active"] = true }
        };
        return context;
    }

    [Fact]
    public void Filter_MultiMatch_ReturnsAllMatchesAsArray()
    {
        var (status, value) = VariableInterpolator.TryResolveJsonPath("$.items[?@.active==true].id", ContextWithItems());

        Assert.Equal(VariableInterpolator.PathResolution.Resolved, status);
        var ids = ((IEnumerable)value!).Cast<object?>().Select(Convert.ToInt32).ToArray();
        Assert.Equal(new[] { 1, 3 }, ids);
    }

    [Fact]
    public void Filter_SingleMatch_ReturnsTheScalarValue()
    {
        var (status, value) = VariableInterpolator.TryResolveJsonPath("$.items[?@.id==2].id", ContextWithItems());

        Assert.Equal(VariableInterpolator.PathResolution.Resolved, status);
        Assert.Equal(2, Convert.ToInt32(value)); // one match → the scalar, not a 1-element array
    }

    [Fact]
    public void Filter_NoMatch_IsMatchedNothing()
    {
        var (status, _) = VariableInterpolator.TryResolveJsonPath("$.items[?@.id==99].id", ContextWithItems());

        Assert.Equal(VariableInterpolator.PathResolution.MatchedNothing, status);
    }
}
