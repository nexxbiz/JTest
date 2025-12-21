using JTest.Cli.Settings;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace JTest.Cli.Commands;

public class RunCommand(IAnsiConsole ansiConsole, IJTestSuiteExecutionResultProcessor testExecutionResultsProcessor, IJTestSuiteExecutor testSuiteExecutor, IVariablesContext variablesContext, JsonSerializerOptionsCache serializerOptionsCache)
    : CommandBase<RunCommandSettings>(ansiConsole)
{
    public const string CommandName = "run";

    protected virtual bool IsDebug => false;

    public override sealed async Task<int> ExecuteAsync(CommandContext context, RunCommandSettings settings, CancellationToken cancellationToken)
    {
        InitializeVariablesContext(settings);

        var results = await ExecuteRunCommand(settings);
        if (results is null)
        {
            return 1;
        }

        var outputDirectory = GetOutputDirectory(settings);
        testExecutionResultsProcessor.Process(results, outputDirectory, IsDebug, settings.SkipOutput == true, settings.OutputFormat);
        if (results.All(x => x.CasesFailed == 0))
        {
            return 0;
        }

        return 1;
    }

    private async Task<IEnumerable<JTestSuiteExecutionResult>?> ExecuteRunCommand(RunCommandSettings settings)
    {
        var testSuites = ReadTestSuites(settings);
        if (!testSuites.Any())
        {
            Console.WriteLine(
                $"Error: No test files found matching patterns: {string.Join(", ", settings.TestFilePatterns ?? [])}",
                new Style(foreground: Color.Red)
            );
            return null;
        }

        if (settings.ParallelTestExecutionCount > 1)
        {
            Console.WriteLine($"Running {testSuites.Count()} test files in parallel (max concurrent: {settings.ParallelTestExecutionCount})");
            return testSuiteExecutor.ExecuteParallel(testSuites, settings.ParallelTestExecutionCount.Value);
        }

        return await testSuiteExecutor.Execute(testSuites);
    }

    private IEnumerable<JTestSuite> ReadTestSuites(RunCommandSettings settings)
    {
        var testFiles = JsonFileSearcher.Search(settings.TestFilePatterns!, settings.GetCategories());

        return testFiles.Select(filePath =>
        {
            var json = File.ReadAllText(filePath);
            var testSuite = JsonSerializer.Deserialize<JTestSuite>(filePath, serializerOptionsCache.Options)
                ?? throw new ArgumentException($"Test suite at path '{filePath}' is not a valid JTestSuite");
            testSuite.FilePath = filePath;

            return testSuite;
        });
    }

    private void InitializeVariablesContext(RunCommandSettings settings)
    {
        var environmentVariables = settings.GetEnvironmentVariables();
        var globalVariables = settings.GetGlobalVariables();
        variablesContext.Initialize(environmentVariables, globalVariables);
    }

    private static string GetOutputDirectory(RunCommandSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputDirectoryPath))
            return settings.OutputDirectoryPath;

        return Directory.GetCurrentDirectory();
    }
}
