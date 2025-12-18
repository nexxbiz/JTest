using JTest.Cli.Options;
using JTest.Cli.Utilities;
using JTest.Core;
using JTest.Core.Models;
using JTest.Core.Utilities;
using Spectre.Console.Cli;
using System.Text.Json;

namespace JTest.Cli.Commands
{
    public class RunCommand(TestExecutionResultsProcessor testExecutionResultsProcessor, JTestSuiteExecutor testSuiteExecutor) : ICommand<RunCommandSettings>
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected virtual bool IsDebug => false;

        public async Task<int> ExecuteAsync(CommandContext context, RunCommandSettings settings, CancellationToken cancellationToken)
        {
            var results = await ExecuteRunCommand(settings);
            if (results is null)
            {
                return 1;
            }

            testExecutionResultsProcessor.ProcessResults(results, settings.OutputDirectoryPath, IsDebug, settings.SkipOutput == true);
            if (results.All(x => x.CasesFailed == 0))
            {
                return 0;
            }

            return 1;
        }

        public Spectre.Console.ValidationResult Validate(CommandContext context, CommandSettings settings)
        {
            return settings.Validate();
        }

        public Task<int> ExecuteAsync(CommandContext context, CommandSettings settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, (RunCommandSettings)settings, cancellationToken);
        }

        private async Task<IEnumerable<TestFileExecutionResult>?> ExecuteRunCommand(RunCommandSettings settings)
        {
            var testSuites = ReadTestSuites(settings);
            if (!testSuites.Any())
            {
                Console.Error.WriteLine($"Error: No test files found matching patterns: {string.Join(", ", settings.TestFilePatterns ?? [])}");
                return null;
            }

            if (settings.ParallelTestExecutionCount > 1)
            {
                Console.WriteLine($"Running {testSuites.Count()} test files in parallel (max concurrent: {settings.ParallelTestExecutionCount})");
                return testSuiteExecutor.ExecuteParallel(testSuites, settings.ParallelTestExecutionCount.Value);
            }

            return await testSuiteExecutor.Execute(testSuites);
        }

        private static IEnumerable<JTestSuite> ReadTestSuites(RunCommandSettings settings)
        {
            var testFiles = TestFileSearcher.Search(settings.TestFilePatterns!, settings.Categories ?? []);

            return testFiles.Select(filePath =>
            {
                var json = File.ReadAllText(filePath);
                var testSuite = JsonSerializer.Deserialize<JTestSuite>(filePath, jsonSerializerOptions)
                    ?? throw new ArgumentException($"Test suite at path '{filePath}' is not a valid JTestSuite");
                testSuite.FilePath = filePath;

                return testSuite;
            });            
        }       
    }
}
