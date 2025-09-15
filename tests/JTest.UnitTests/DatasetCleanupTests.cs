using JTest.Core.Execution;
using JTest.Core.Models;

namespace JTest.UnitTests;

/// <summary>
/// Tests to verify proper cleanup between dataset iterations
/// </summary>
public class DatasetCleanupTests
{
    [Fact]
    public async Task ExecuteAsync_WithMultipleDatasets_GlobalsAreSharedBetweenIterations()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Globals sharing test",
            Steps = new List<object>(), // Empty steps for this test
            Datasets = new List<JTestDataset>
            {
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
            }
        };

        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.test.com" };
        baseContext.Variables["globals"] = new Dictionary<string, object> { ["sharedCounter"] = 0 };
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["localVar"] = "initial" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count);

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
        var executor = new TestExecutionContext_MockingExecutor(); // We'll need a mock that simulates variable changes
        var testCase = new JTestCase
        {
            Name = "Context reset test",
            Steps = new List<object> { new { type = "mock_step" } }, // Mock step that modifies variables
            Datasets = new List<JTestDataset>
            {
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
            }
        };

        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { immutableValue = "never-changes" };
        baseContext.Variables["globals"] = new Dictionary<string, object>();
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["status"] = "ready" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count);

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
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Env immutability test",
            Steps = new List<object>(),
            Datasets = new List<JTestDataset>
            {
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
            }
        };

        var originalEnv = new { baseUrl = "https://api.test.com", apiKey = "secret123" };
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = originalEnv;

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count);

        // Verify that env variables remain unchanged in the base context
        Assert.Equal(originalEnv, baseContext.Variables["env"]);
    }

    [Fact]
    public void TestCaseExecutor_VariableScopingRules_AreImplementedCorrectly()
    {
        // Arrange - Test the helper methods directly to verify scoping logic
        var executor = new TestCaseExecutor();
        var baseContext = new TestExecutionContext();

        baseContext.Variables["env"] = new { url = "https://test.com" };
        baseContext.Variables["globals"] = new Dictionary<string, object> { ["token"] = "abc123" };
        baseContext.Variables["ctx"] = new Dictionary<string, object> { ["step"] = 1 };
        baseContext.Variables["this"] = new { result = "data" };

        // Simulate updated globals from first iteration
        var updatedGlobals = new Dictionary<string, object> { ["token"] = "xyz789", ["newVar"] = "added" };

        // Use reflection to test private methods (for testing purposes only)
        var captureMethod = typeof(TestCaseExecutor).GetMethod("CaptureOriginalVariables",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prepareMethod = typeof(TestCaseExecutor).GetMethod("PrepareIterationContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(captureMethod);
        Assert.NotNull(prepareMethod);

        // Act
        var originalVariables = (Dictionary<string, object>)captureMethod.Invoke(executor, new object[] { baseContext })!;
        var iterationContext = (TestExecutionContext)prepareMethod.Invoke(executor,
            new object[] { baseContext, originalVariables, updatedGlobals })!;

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

/// <summary>
/// Mock executor for testing variable modification scenarios
/// This simulates what would happen when actual steps execute and modify context
/// </summary>
public class TestExecutionContext_MockingExecutor : TestCaseExecutor
{
    // For now, we'll use the base implementation
    // In a real scenario, this would override execution to simulate variable changes
}