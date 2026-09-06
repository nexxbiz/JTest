using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using Xunit;

namespace JTest.UnitTests.Steps;

public class LoopRetentionTests
{
    private static ForLoopStep ForLoop(ForLoopStepConfiguration c) => new(StepProcessor.Default, c);

    [Fact]
    public async Task ForLoop_RetainsEveryIteration_WithItsOwnSteps()
    {
        var context = new TestExecutionContext();
        var config = new ForLoopStepConfiguration(
            new object[] { "a", "b", "c" },
            [new WaitStep(new(Ms: 1))]);

        var result = await ForLoop(config).ExecuteAsync(context, default);

        var iterations = result.Iterations.ToList();
        Assert.Equal(3, iterations.Count);                                  // all iterations kept
        Assert.Equal(new[] { 0, 1, 2 }, iterations.Select(i => i.Index).ToArray());
        Assert.All(iterations, it => Assert.Single(it.Steps));              // each keeps its own step
        Assert.All(iterations, it => Assert.True(it.Success));
        Assert.Equal(3, result.InnerProcessedResults.Count());             // no overwrite/loss
    }

    [Fact]
    public async Task ForLoop_EarlyExit_KeepsExactExecutedCount_NoFabricatedRemainder()
    {
        var context = new TestExecutionContext();
        var config = new ForLoopStepConfiguration(
            new object[] { "a", "b", "c" },
            [
                new WaitStep(new(Ms: 1)),
                new WaitStep(new(Ms: "invalid value type")) // fails on iteration 0
            ]);

        var result = await ForLoop(config).ExecuteAsync(context, default);

        var iterations = result.Iterations.ToList();
        Assert.Single(iterations);                    // only the executed iteration; remainder NOT fabricated
        Assert.False(iterations[0].Success);
        Assert.Equal(2, iterations[0].Steps.Count);   // ran step 1 (ok) then step 2 (failed), no stale slots
        Assert.True(iterations[0].Steps[0].Success);
        Assert.False(iterations[0].Steps[1].Success);
        Assert.Equal(0, result.Data!["completedIterationCount"]);
    }
}
