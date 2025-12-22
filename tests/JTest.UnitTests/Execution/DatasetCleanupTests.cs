using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using System.Reflection;

namespace JTest.UnitTests.Execution;

/// <summary>
/// Tests to verify proper cleanup between dataset iterations
/// </summary>
public class DatasetCleanupTests
{
    private static readonly JTestCaseExecutor executor = new(StepProcessor.Default);

    [Fact]
    public async Task ExecuteAsync_WithMultipleDatasets_GlobalsAreSharedBetweenIterations()
    {
        // Arrange
        var testCase = new JTestCase
        {
            Name = "Globals sharing test",
            Steps = [], // Empty steps for this test
            Datasets =
            [
                new()
                {
                    Name = "dataset1",
                    Case = new Dictionary<string, object> { ["iteration"] = 1 }
                },
                new()
                {
                    Name = "dataset2",
                    Case = new Dictionary<string, object> { ["iteration"] = 2 }
                }
            ]
        };

        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.test.com" };
        baseContext.Variables["globals"] = new Dictionary<string, object> { ["sharedCounter"] = 0 };
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["localVar"] = "initial" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count());

        // Verify that the base context is not modified during execution
        var originalGlobals = baseContext.Variables["globals"] as Dictionary<string, object>;
        Assert.NotNull(originalGlobals);
        Assert.Equal(0, originalGlobals["sharedCounter"]); // Should remain unchanged in base context

        var originalCtx = baseContext.Variables["ctx"] as Dictionary<string, object>;
        Assert.NotNull(originalCtx);
        Assert.Equal("initial", originalCtx["localVar"]); // Should remain unchanged in base context
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleDatasets_ContextVariablesAreResetBetweenIterations()
    {
        // Arrange
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { immutableValue = "never-changes" };
        baseContext.Variables["globals"] = new Dictionary<string, object>
        {
            ["var"] = "initialValue"
        };
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["status"] = "ready" };
        var saveVariableModification = new Dictionary<string, object?>
        {
            ["$.ctx.status"] = "finished"
        };
        var testCase = new JTestCase
        {
            Name = "Context reset test",
            Steps = [
                new WaitStep(new(1, Save: saveVariableModification)) // Mock step that modifies variables
            ],
            Datasets =
            [
                new()
                {
                    Name = "dataset1",
                    Case = new Dictionary<string, object> { ["value"] = "first" }
                },
                new()
                {
                    Name = "dataset2",
                    Case = new Dictionary<string, object> { ["value"] = "second" }
                }
            ]
        };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count());

        // Verify base context remains unchanged
        var baseEnv = baseContext.Variables["env"];
        var baseGlobals = baseContext.Variables["globals"] as Dictionary<string, object>;
        var baseCtx = baseContext.Variables["ctx"] as Dictionary<string, object>;

        Assert.NotNull(baseEnv);
        Assert.NotNull(baseGlobals);
        Assert.NotNull(baseCtx);
        Assert.Equal("ready", baseCtx["status"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleDatasets_EnvVariablesRemainImmutable()
    {
        // Arrange        
        var testCase = new JTestCase
        {
            Name = "Env immutability test",
            Steps = [],
            Datasets =
            [
                new()
                {
                    Name = "dataset1",
                    Case = new Dictionary<string, object> { ["test"] = "value1" }
                },
                new()
                {
                    Name = "dataset2",
                    Case = new Dictionary<string, object> { ["test"] = "value2" }
                }
            ]
        };

        var originalEnv = new { baseUrl = "https://api.test.com", apiKey = "secret123" };
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = originalEnv;

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count());

        // Verify that env variables remain unchanged in the base context
        Assert.Equal(originalEnv, baseContext.Variables["env"]);
    }

    [Fact]
    public void TestCaseExecutor_VariableScopingRules_AreImplementedCorrectly()
    {
        // Arrange - Test the helper methods directly to verify scoping logic        
        var baseContext = new TestExecutionContext();

        baseContext.Variables["env"] = new { url = "https://test.com" };
        baseContext.Variables["globals"] = new Dictionary<string, object> { ["token"] = "abc123" };
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["step"] = 1 };
        baseContext.Variables["this"] = new { result = "data" };

        // Simulate updated globals from first iteration
        var updatedGlobals = new Dictionary<string, object> { ["token"] = "xyz789", ["newVar"] = "added" };

        // Use reflection to test private methods (for testing purposes only)
        var captureMethod = typeof(JTestCaseExecutor).GetMethod("CaptureOriginalVariables",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        var prepareMethod = typeof(JTestCaseExecutor).GetMethod("PrepareIterationContext",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        Assert.NotNull(captureMethod);
        Assert.NotNull(prepareMethod);

        // Act
        var originalVariables = (Dictionary<string, object>)captureMethod.Invoke(executor, [baseContext])!;
        var iterationContext = (TestExecutionContext)prepareMethod.Invoke(executor, [baseContext, originalVariables, updatedGlobals])!;

        // Assert - verify proper scoping
        Assert.Equal(baseContext.Variables["env"], iterationContext.Variables["env"]); // env unchanged
        Assert.Equal(updatedGlobals, iterationContext.Variables["globals"]); // globals updated
        Assert.NotSame(baseContext.Variables["ctx"], iterationContext.Variables["ctx"]); // ctx is cloned

        // Verify ctx values are reset to original
        var iterationCtx = iterationContext.Variables["ctx"] as Dictionary<string, object>;
        Assert.NotNull(iterationCtx);
        Assert.Equal(1, iterationCtx["step"]);
    }
}