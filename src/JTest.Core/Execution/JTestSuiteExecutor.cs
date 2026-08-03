using JTest.Core.Models;
using JTest.Core.Templates;
using JTest.Core.Variables;
using Spectre.Console;
using System.Collections.Concurrent;

namespace JTest.Core.Execution;

public sealed class JTestSuiteExecutor(IJTestCaseExecutor testCaseExecutor, IVariablesContext variablesContext, ITemplateContext templateContext, IAnsiConsole console)
    : IJTestSuiteExecutor
{
    public async Task<IEnumerable<JTestSuiteExecutionResult>> Execute(IEnumerable<JTestSuite> testFiles, CancellationToken cancellationToken = default)
    {
        var allResults = new List<JTestSuiteExecutionResult>();

        foreach (var testFile in testFiles)
        {
            // Cancellation stops the run; the current and remaining suites are recorded as cancelled
            // (a distinct outcome that fails the run — FR-006) rather than silently dropped.
            if (cancellationToken.IsCancellationRequested)
            {
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    Cancelled = true
                });
                continue;
            }

            console.WriteLine($"Running test file: {testFile.FilePath}", new Style(foreground: Color.GreenYellow));

            try
            {
                var executionResults = await RunTestSuiteAsync(testFile);
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, executionResults));
                
                if(executionResults.All(x => x.Success))
                    console.WriteLine($"Test file {testFile.FilePath} passed", new Style(foreground: Color.Green));
                else
                    console.WriteLine($"Test file {testFile.FilePath} failed", new Style(foreground: Color.Red));

                console.WriteLine();
            }
            catch (OperationCanceledException)
            {
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    Cancelled = true
                });
            }
            catch (Exception ex)
            {
                console.WriteLine();
                console.WriteLine($"Test file {testFile.FilePath} failed", new Style(foreground: Color.Red));
                console.WriteException(ex, ExceptionFormats.NoStackTrace);

                // Never drop a crashing suite — capture it as errored so it fails the run (FR-002).
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    ExecutionError = ex.Message
                });
            }
        }

        return allResults;
    }

    public IEnumerable<JTestSuiteExecutionResult> ExecuteParallel(IEnumerable<JTestSuite> testFiles, int parallelCount, CancellationToken cancellationToken = default)
    {
        // Thread-safe collections for parallel execution
        var allResults = new ConcurrentBag<JTestSuiteExecutionResult>();

        var processedFilesThreadSafe = 0;
        var failedFilesThreadSafe = 0;

        // Use Parallel.ForEach with MaxDegreeOfParallelism
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelCount
        };

        Parallel.ForEach(testFiles, parallelOptions, testFile =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    Cancelled = true
                });
                return;
            }

            lock (Console.Out)
            {
                Console.WriteLine($"Running test file: {testFile}");
            }

            try
            {
                // Read and execute the test suite
                var executionResults = RunTestSuiteAsync(testFile).Result;
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, executionResults));

                Interlocked.Increment(ref processedFilesThreadSafe);
            }
            catch (OperationCanceledException)
            {
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    Cancelled = true
                });
            }
            catch (Exception ex)
            {
                lock (Console.Error)
                {
                    Console.Error.WriteLine($"Error executing test file {testFile}: {ex.Message}");
                }

                // Never drop a crashing suite in the parallel path either (FR-002/FR-005).
                allResults.Add(new(testFile.FilePath, testFile.Info?.Name, testFile.Info?.Description, Array.Empty<JTestCaseResult>())
                {
                    ExecutionError = ex.Message
                });
                Interlocked.Increment(ref failedFilesThreadSafe);
            }
        });

        return allResults;
    }

    private async Task<IEnumerable<JTestCaseResult>> RunTestSuiteAsync(JTestSuite testSuite)
    {
        // Merge environment variables (parameter takes precedence)
        var mergedEnvironment = MergeDictionaries(testSuite.Env, variablesContext.EnvironmentVariables);

        // Merge global variables (parameter takes precedence)
        var mergedGlobals = MergeDictionaries(testSuite.Globals, variablesContext.GlobalVariables);

        // Load templates for test suite
        await templateContext.Load(testSuite);

        var allResults = new List<JTestCaseResult>();

        // Execute each test case in the suite
        int testNumber = 1;
        foreach (var testCase in testSuite.Tests)
        {
            var context = CreateExecutionContext(mergedEnvironment, mergedGlobals);
            var results = await testCaseExecutor.ExecuteAsync(testCase, context, testNumber);
            allResults.AddRange(results);
            testNumber++;
        }

        return allResults;
    }

    private static TestExecutionContext CreateExecutionContext(Dictionary<string, object?>? environmentVariables, Dictionary<string, object?>? globalVariables)
    {
        var context = new TestExecutionContext();
        context.Variables["env"] = environmentVariables ?? [];
        context.Variables["globals"] = globalVariables ?? [];
        context.Variables["ctx"] = new Dictionary<string, object?>();

        return context;
    }

    private static Dictionary<string, object?> MergeDictionaries(IReadOnlyDictionary<string, object?>? source, IReadOnlyDictionary<string, object?>? target)
    {
        var result = new Dictionary<string, object?>();

        // Add source first
        if (source != null)
        {
            foreach (var kvp in source)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // Add target, overriding source values
        if (target != null)
        {
            foreach (var kvp in target)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }
}
