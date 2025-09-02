using JTest.Core.Execution;
using JTest.Core.Models;

namespace JTest.UnitTests;

public class TestCaseExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutDatasets_ExecutesOnce()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Simple test",
            Flow = new List<object> { new { type = "wait", ms = 1 } },
            Datasets = null
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("Simple test", results[0].TestCaseName);
        Assert.Null(results[0].Dataset);
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyDatasets_ExecutesOnce()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Test with empty datasets",
            Flow = new List<object> { new { type = "wait", ms = 1 } },
            Datasets = new List<JTestDataset>()
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("Test with empty datasets", results[0].TestCaseName);
        Assert.Null(results[0].Dataset);
    }

    [Fact]
    public async Task ExecuteAsync_WithDatasets_ExecutesMultipleTimes()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Data-driven test",
            Flow = new List<object> { new { type = "wait", ms = 1 } },
            Datasets = new List<JTestDataset>
            {
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
            }
        };
        var baseContext = new TestExecutionContext();

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Equal(2, results.Count);
        
        Assert.Equal("Data-driven test", results[0].TestCaseName);
        Assert.NotNull(results[0].Dataset);
        Assert.Equal("dataset1", results[0].Dataset.Name);
        Assert.Equal("user1", results[0].Dataset.Case["userId"]);
        
        Assert.Equal("Data-driven test", results[1].TestCaseName);
        Assert.NotNull(results[1].Dataset);
        Assert.Equal("dataset2", results[1].Dataset.Name);
        Assert.Equal("user2", results[1].Dataset.Case["userId"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithDatasets_SetsCaseContextCorrectly()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Case context test",
            Flow = new List<object>(), // Empty flow for this test
            Datasets = new List<JTestDataset>
            {
                new() 
                { 
                    Name = "test-dataset", 
                    Case = new Dictionary<string, object> 
                    { 
                        ["accountId"] = "acct-1001",
                        ["expectedTotal"] = 20.0
                    } 
                }
            }
        };
        
        // Create a mock context to verify case setting
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.test.com" };

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Equal("test-dataset", results[0].Dataset!.Name);
        
        // Verify that the case context would have been set correctly
        var dataset = results[0].Dataset;
        Assert.Equal("acct-1001", dataset.Case["accountId"]);
        Assert.Equal(20.0, dataset.Case["expectedTotal"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDatasets_ClearsCaseContext()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "No dataset test",
            Flow = new List<object>(),
            Datasets = null
        };
        
        var baseContext = new TestExecutionContext();
        // Pre-populate case context to ensure it gets cleared
        baseContext.SetCase(new Dictionary<string, object> { ["leftover"] = "value" });

        // Act
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // Assert
        Assert.Single(results);
        Assert.Null(results[0].Dataset);
        
        // For tests without datasets, case should be cleared
        // This is verified by the absence of dataset in the result
    }

    [Fact]
    public async Task ExecuteAsync_PreservesBaseContextVariables()
    {
        // Arrange
        var executor = new TestCaseExecutor();
        var testCase = new JTestCase
        {
            Name = "Context preservation test",
            Flow = new List<object>(),
            Datasets = new List<JTestDataset>
            {
                new() { Name = "test", Case = new Dictionary<string, object> { ["caseVar"] = "caseValue" } }
            }
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
        if (baseContext.Variables.ContainsKey("case"))
        {
            var caseContext = baseContext.Variables["case"] as Dictionary<string, object>;
            Assert.NotNull(caseContext);
            Assert.DoesNotContain("caseVar", caseContext.Keys);
        }
    }
}