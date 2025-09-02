using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.Core.Execution;

/// <summary>
/// Service for executing test cases with dataset support
/// </summary>
public class TestCaseExecutor
{
    /// <summary>
    /// Executes a test case, running it once if no datasets are provided,
    /// or multiple times (once per dataset) if datasets are available
    /// </summary>
    /// <param name="testCase">The test case to execute</param>
    /// <param name="baseContext">The base execution context with environment and global variables</param>
    /// <returns>List of test case results (one per dataset, or one if no datasets)</returns>
    public async Task<List<JTestCaseResult>> ExecuteAsync(JTestCase testCase, TestExecutionContext baseContext)
    {
        var results = new List<JTestCaseResult>();

        if (testCase.Datasets == null || testCase.Datasets.Count == 0)
        {
            // Execute test case once without dataset
            var result = await ExecuteTestCaseAsync(testCase, baseContext, null);
            results.Add(result);
        }
        else
        {
            // Execute test case multiple times with datasets
            results.AddRange(await RunTestWithDatasetsAsync(testCase, baseContext));
        }

        return results;
    }

    /// <summary>
    /// Runs the test case multiple times with each dataset
    /// </summary>
    private async Task<List<JTestCaseResult>> RunTestWithDatasetsAsync(JTestCase testCase, TestExecutionContext baseContext)
    {
        var results = new List<JTestCaseResult>();

        foreach (var dataset in testCase.Datasets!)
        {
            var result = await ExecuteTestCaseAsync(testCase, baseContext, dataset);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Executes a single test case iteration with an optional dataset
    /// </summary>
    private async Task<JTestCaseResult> ExecuteTestCaseAsync(JTestCase testCase, TestExecutionContext baseContext, JTestDataset? dataset)
    {
        var startTime = DateTime.UtcNow;
        var result = new JTestCaseResult
        {
            TestCaseName = testCase.Name,
            Dataset = dataset
        };

        try
        {
            // Create a copy of the base context for this execution
            var executionContext = CloneContext(baseContext);

            // Set case data if dataset is provided
            if (dataset != null)
            {
                executionContext.SetCase(dataset.Case);
            }
            else
            {
                executionContext.ClearCase();
            }

            // Execute the test flow steps
            foreach (var stepConfig in testCase.Flow)
            {
                // TODO: This would need to be implemented with step factory/registry
                // For now, this is a placeholder showing the execution pattern
                
                // Parse step configuration and create step instance
                // var step = stepFactory.CreateStep(stepConfig);
                // var stepResult = await step.ExecuteAsync(executionContext);
                // result.StepResults.Add(stepResult);
                
                // if (!stepResult.Success)
                // {
                //     result.Success = false;
                //     result.ErrorMessage = stepResult.ErrorMessage;
                //     break;
                // }
            }

            result.Success = true; // Placeholder - would be determined by step results
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// Creates a copy of the base context for test execution
    /// </summary>
    private TestExecutionContext CloneContext(TestExecutionContext baseContext)
    {
        var clonedContext = new TestExecutionContext();

        // Copy all variables from base context
        foreach (var kvp in baseContext.Variables)
        {
            clonedContext.Variables[kvp.Key] = kvp.Value;
        }

        // Copy log entries
        foreach (var logEntry in baseContext.Log)
        {
            clonedContext.Log.Add(logEntry);
        }

        return clonedContext;
    }
}