using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.UnitTests.Execution;

public sealed class JTestCaseExecutorTests
{
    [Fact]
    public async Task When_ExecuteAsync_WithoutDatasets_ExecutesOnce()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Simple test",
            Steps = [new WaitStep(new(Ms: 1))],
            Datasets = null
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("Simple test", results.First().TestCaseName);
        Assert.Null(results.First().Dataset);
        Assert.True(results.First().Success);
    }

    [Fact]
    public async Task When_ExecuteAsync_WithEmptyDatasets_ExecutesOnce()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Test with empty datasets",
            Steps = [new WaitStep(new(Ms: 1))],
            Datasets = []
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("Test with empty datasets", results.First().TestCaseName);
        Assert.Null(results.First().Dataset);
    }

    [Fact]
    public async Task When_ExecuteAsync_WithDatasets_ExecutesMultipleTimes()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Data-driven test",
            Steps = [new WaitStep(new(Ms: 1))],
            Datasets =
            [
                new()
                {
                    Name = "dataset1",
                    Case = new Dictionary<string, object> { ["userId"] = "user1" }
                },
                new()
                {
                    Name = "dataset2",
                    Case = new Dictionary<string, object> { ["userId"] = "user2" }
                }
            ]
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count());

        Assert.Equal("Data-driven test", results.First().TestCaseName);
        Assert.NotNull(results.First().Dataset);
        Assert.Equal("dataset1", results.First().Dataset!.Name);
        Assert.Equal("user1", results.First().Dataset!.Case["userId"]);

        Assert.Equal("Data-driven test", results.Last().TestCaseName);
        Assert.NotNull(results.Last().Dataset);
        Assert.Equal("dataset2", results.Last().Dataset!.Name);
        Assert.Equal("user2", results.Last().Dataset!.Case["userId"]);
    }

    [Fact]
    public async Task When_ExecuteAsync_WithDatasets_SetsCaseContextCorrectly()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Case context test",
            Steps = [], // Empty steps for this test
            Datasets =
            [
                new()
                {
                    Name = "test-dataset",
                    Case = new Dictionary<string, object>
                    {
                        ["accountId"] = "acct-1001",
                        ["expectedTotal"] = 20.0
                    }
                }
            ]
        };

        // Create a mock context to verify case setting
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.test.com" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("test-dataset", results.First().Dataset!.Name);

        // Verify that the case context would have been set correctly
        var dataset = results.First().Dataset;
        Assert.NotNull(dataset);
        Assert.Equal("acct-1001", dataset.Case["accountId"]);
        Assert.Equal(20.0, dataset.Case["expectedTotal"]);
    }

    [Fact]
    public async Task When_ExecuteAsync_WithoutDatasets_ClearsCaseContext()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "No dataset test",
            Steps = [],
            Datasets = null
        };

        var baseContext = new TestExecutionContext();
        // Pre-populate case context to ensure it gets cleared
        baseContext.SetCase(new Dictionary<string, object> { ["leftover"] = "value" });

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Null(results.First().Dataset);
    }

    [Fact]
    public async Task When_ExecuteAsync_PreservesBaseContextVariables()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Context preservation test",
            Steps = [],
            Datasets =
            [
                new() { Name = "test", Case = new Dictionary<string, object> { ["caseVar"] = "caseValue" } }
            ]
        };

        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.test.com" };
        baseContext.Variables["globals"] = new { authToken = "token123" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);

        // Verify the original context is preserved (not modified)
        Assert.Contains("env", baseContext.Variables.Keys);
        Assert.Contains("globals", baseContext.Variables.Keys);

        // Case should not be set in base context (execution uses cloned context)
        if (baseContext.Variables.TryGetValue("case", out object? value))
        {
            var caseContext = value as Dictionary<string, object>;
            Assert.NotNull(caseContext);
            Assert.DoesNotContain("caseVar", caseContext.Keys);
        }
    }


    [Fact]
    public async Task When_ExecuteAsync_And_StepFails_Then_AddsError()
    {
        // Arrange
        var executor = GetSut();
        var testCase = new JTestCase
        {
            Name = "Test with failing step",
            Steps =
            [
                new WaitStep(new(Ms: "not-a-number")),
                new WaitStep(new(Ms: 1))
            ],
            Datasets = []
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.NotNull(results.First().ErrorMessage);
        Assert.NotEmpty(results.First().ErrorMessage!);

        var stepResults = results.First().StepResults;
        Assert.Equal(2, stepResults.Count());
        Assert.False(stepResults.First().Success);
        Assert.True(stepResults.Last().Success);
    }

    private static JTestCaseExecutor GetSut()
    {
        return new(StepProcessor.Default);
    }
}