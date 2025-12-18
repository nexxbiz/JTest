using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.Core
{
    public sealed class JTestCaseExecutor(StepFactory? stepFactory = null)
    {
        private readonly StepFactory _stepFactory = stepFactory ?? new StepFactory();

        /// <summary>
        /// Executes a test case, running it once if no datasets are provided,
        /// or multiple times (once per dataset) if datasets are available
        /// </summary>
        /// <param name="testCase">The test case to execute</param>
        /// <param name="baseContext">The base execution context with environment and global variables</param>
        /// <param name="testNumber">The test number for debug logging</param>
        /// <returns>List of test case results (one per dataset, or one if no datasets)</returns>
        public async Task<IEnumerable<JTestCaseResult>> ExecuteAsync(JTestCase testCase, TestExecutionContext baseContext, int testNumber = 1)
        {
            var results = new List<JTestCaseResult>();

            if (testCase.Datasets == null || testCase.Datasets.Count == 0)
            {
                // Execute test case once without dataset - use cloned context for isolation
                var executionContext = CloneContext(baseContext);
                executionContext.TestNumber = testNumber;
                var result = await ExecuteTestCaseAsync(testCase, executionContext, null);
                results.Add(result);
            }
            else
            {
                // Execute test case multiple times with datasets
                results.AddRange(await RunTestWithDatasetsAsync(testCase, baseContext, testNumber));
            }

            return results;
        }

        /// <summary>
        /// Runs the test case multiple times with each dataset
        /// </summary>
        private async Task<List<JTestCaseResult>> RunTestWithDatasetsAsync(JTestCase testCase, TestExecutionContext baseContext, int testNumber)
        {
            var results = new List<JTestCaseResult>();

            // Capture original variable state for proper cleanup between iterations
            var originalVariables = CaptureOriginalVariables(baseContext);

            // Track globals across iterations (shared state)
            Dictionary<string, object>? sharedGlobals = null;

            foreach (var dataset in testCase.Datasets!)
            {
                // Prepare context for this iteration with proper variable scoping
                var iterationContext = PrepareIterationContext(baseContext, originalVariables, sharedGlobals);
                iterationContext.TestNumber = testNumber;

                var result = await ExecuteTestCaseAsync(testCase, iterationContext, dataset);
                results.Add(result);

                // Capture updated globals for next iteration
                if (iterationContext.Variables.TryGetValue("globals", out object? value))
                {
                    sharedGlobals = value as Dictionary<string, object>;
                }
            }

            return results;
        }

        /// <summary>
        /// Executes a single test case iteration with an optional dataset
        /// </summary>
        private async Task<JTestCaseResult> ExecuteTestCaseAsync(JTestCase testCase, TestExecutionContext executionContext, JTestDataset? dataset)
        {
            var startTime = DateTime.UtcNow;
            var result = new JTestCaseResult
            {
                TestCaseName = testCase.Name,
                Dataset = dataset
            };

            try
            {
                // Set the test case name in the execution context
                executionContext.TestCaseName = testCase.Name;

                // Set case data if dataset is provided
                if (dataset != null)
                {
                    executionContext.SetCase(dataset.Case);
                }
                else
                {
                    executionContext.ClearCase();
                }

                // Execute the test steps
                int stepNumber = 1;
                foreach (var stepConfig in testCase.Steps)
                {
                    try
                    {
                        // Set current step number
                        executionContext.StepNumber = stepNumber;

                        // Create step instance from configuration
                        var step = _stepFactory.CreateStep(stepConfig);
                        var stepResult = await step.ExecuteAsync(executionContext);
                        result.StepResults.Add(stepResult);

                        if (!stepResult.Success)
                        {
                            result.AddError(stepResult.ErrorMessage);
                        }

                        stepNumber++;
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Unexpected exception thrown: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Unexpected exception thrown during execution of steps: {ex.Message}");
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
        private static TestExecutionContext CloneContext(TestExecutionContext baseContext)
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

        /// <summary>
        /// Captures the original state of variables before dataset iterations
        /// This allows proper cleanup between iterations
        /// </summary>
        private Dictionary<string, object> CaptureOriginalVariables(TestExecutionContext baseContext)
        {
            var originalVariables = new Dictionary<string, object>();

            foreach (var kvp in baseContext.Variables)
            {
                // Deep clone the values to preserve original state
                originalVariables[kvp.Key] = DeepCloneVariable(kvp.Value);
            }

            return originalVariables;
        }

        /// <summary>
        /// Prepares execution context for a dataset iteration with proper variable scoping:
        /// - env: immutable, preserved from original (no cloning needed)
        /// - globals: shared across iterations (persisted from previous iteration)
        /// - other variables (ctx, etc.): reset to original values for each iteration
        /// </summary>
        private TestExecutionContext PrepareIterationContext(
            TestExecutionContext baseContext,
            Dictionary<string, object> originalVariables,
            Dictionary<string, object>? sharedGlobals)
        {
            var iterationContext = new TestExecutionContext();

            // Copy log entries from base context
            foreach (var logEntry in baseContext.Log)
            {
                iterationContext.Log.Add(logEntry);
            }

            // Set up variables with proper scoping rules
            foreach (var kvp in originalVariables)
            {
                switch (kvp.Key.ToLowerInvariant())
                {
                    case "env":
                        // env is immutable - use original reference (no cloning needed)
                        iterationContext.Variables[kvp.Key] = kvp.Value;
                        break;

                    case "globals":
                        // globals are shared across iterations - use updated value if available
                        iterationContext.Variables[kvp.Key] = sharedGlobals ?? kvp.Value;
                        break;

                    default:
                        // all other variables (ctx, this, named variables) - reset to original
                        iterationContext.Variables[kvp.Key] = DeepCloneVariable(kvp.Value);
                        break;
                }
            }

            // Ensure globals exists even if not in original variables
            if (!iterationContext.Variables.ContainsKey("globals"))
            {
                iterationContext.Variables["globals"] = sharedGlobals ?? new Dictionary<string, object>();
            }

            return iterationContext;
        }

        /// <summary>
        /// Creates a deep clone of a variable value to prevent reference sharing
        /// </summary>
        private object DeepCloneVariable(object value)
        {
            if (value == null) return null!;

            // For simple types, return as-is
            if (value.GetType().IsValueType || value is string)
            {
                return value;
            }

            // For dictionaries, create a new dictionary with cloned values
            if (value is Dictionary<string, object> dict)
            {
                var clonedDict = new Dictionary<string, object>();
                foreach (var kvp in dict)
                {
                    clonedDict[kvp.Key] = DeepCloneVariable(kvp.Value);
                }
                return clonedDict;
            }

            // For anonymous types and other reference types, use JSON serialization for deep cloning
            // This is a simple approach that works for most serializable objects
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(value);
                // For env variables and other immutable objects, we want to preserve the original type
                // rather than deserializing to JsonElement, so we return the original value
                // This is safe for immutable objects like env
                return value;
            }
            catch
            {
                // If serialization fails, return the original (not ideal but safe)
                return value;
            }
        }
    }
}
