using JTest.Core.Execution;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Xunit;

namespace JTest.UnitTests.Variables;

/// <summary>
/// <c>$.run</c> exists so a suite that creates server-side resources with globally-unique identity
/// can generate a fresh value per run and stay re-runnable, instead of passing once and conflicting
/// forever. The values are stable within a run so a create step and a later fetch step agree.
/// </summary>
public class RunVariablesTests
{
    [Fact]
    public void RunVariables_AreStableWithinARun()
    {
        var context = new VariablesContext();

        var first = context.RunVariables;
        var second = context.RunVariables;

        Assert.Equal(first["uuid"], second["uuid"]);
        Assert.Equal(first["id"], second["id"]);
        Assert.Equal(first["timestamp"], second["timestamp"]);
    }

    [Fact]
    public void RunVariables_DifferBetweenRuns()
    {
        var first = new VariablesContext().RunVariables;
        var second = new VariablesContext().RunVariables;

        Assert.NotEqual(first["uuid"], second["uuid"]);
    }

    [Fact]
    public void RunVariables_ExposeUsableShapes()
    {
        var run = new VariablesContext().RunVariables;

        Assert.True(Guid.TryParse(run["uuid"]!.ToString(), out _));
        Assert.Equal(8, run["id"]!.ToString()!.Length);
        Assert.True(DateTimeOffset.TryParse(run["timestamp"]!.ToString(), out _));
        Assert.True(Convert.ToInt64(run["epoch"]) > 0);
        Assert.True(Convert.ToInt64(run["epochMs"]) > 0);
    }

    [Fact]
    public void RunVariables_ResolveThroughJsonPath()
    {
        var run = new VariablesContext().RunVariables;
        var context = new TestExecutionContext();
        context.Variables["run"] = new Dictionary<string, object?>(run);

        var resolved = VariableInterpolator.ResolveVariableTokens("route-{{$.run.id}}", context, out var unresolved);

        Assert.Empty(unresolved);
        Assert.Equal($"route-{run["id"]}", resolved);
    }
}
