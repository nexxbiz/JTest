using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace JTest.Cli.Commands;

public class RunCommand : CommandBase<RunCommandSettings>
{
    private readonly IJTestSuiteExecutionResultProcessor _testExecutionResultsProcessor;
    private readonly IJTestSuiteExecutor _testSuiteExecutor;
    private readonly IVariablesContext _variablesContext;
    private readonly ITemplateContext _templateContext;
    private readonly JsonSerializerOptionsAccessor _serializerOptionsCache;

    protected virtual bool IsDebug => false;

    public RunCommand(
        IAnsiConsole ansiConsole,
        IErrorHandlingService errorHandlingService,
        IJTestSuiteExecutionResultProcessor testExecutionResultsProcessor,
        IJTestSuiteExecutor testSuiteExecutor,
        IVariablesContext variablesContext,
        ITemplateContext templateContext,
        JsonSerializerOptionsAccessor serializerOptionsCache)
        : base(ansiConsole, errorHandlingService)
    {
        _testExecutionResultsProcessor = testExecutionResultsProcessor;
        _testSuiteExecutor = testSuiteExecutor;
        _variablesContext = variablesContext;
        _templateContext = templateContext;
        _serializerOptionsCache = serializerOptionsCache;
    }

    public sealed override async Task<int> ExecuteAsync(CommandContext context, RunCommandSettings settings, CancellationToken cancellationToken)
    {
        await _templateContext.LoadGlobalTemplates();

        InitializeVariablesContext(settings);

        var results = await ExecuteRunCommand(settings);
        if (results is null)
        {
            // No test files matched — a run that produced nothing is not success (FR-003).
            return RunResultEvaluator.ExitCode(Array.Empty<JTestSuiteExecutionResult>(), noFilesMatched: true);
        }

        var resultList = results as IReadOnlyList<JTestSuiteExecutionResult> ?? results.ToList();

        var outputDirectory = GetOutputDirectory(settings);
        _testExecutionResultsProcessor.Process(resultList, outputDirectory, IsDebug, settings.SkipOutput == true, settings.OutputFormat);

        return RunResultEvaluator.ExitCode(resultList, noFilesMatched: false);
    }

    private async Task<IEnumerable<JTestSuiteExecutionResult>?> ExecuteRunCommand(RunCommandSettings settings)
    {
        var testSuites = ReadTestSuites(settings);
        var jTestSuites = testSuites as JTestSuite[] ?? testSuites.ToArray();
        if (jTestSuites.Length == 0)
        {
            Console.WriteLine(
                $"Error: No test files found matching patterns: {string.Join(", ", settings.TestFilePatterns)}",
                new Style(foreground: Color.Red)
            );
            return null;
        }

        if (settings.ParallelTestExecutionCount > 1)
        {
            Console.WriteLine($"Running {jTestSuites.Length} test files in parallel (max concurrent: {settings.ParallelTestExecutionCount})");
            return _testSuiteExecutor.ExecuteParallel(jTestSuites, settings.ParallelTestExecutionCount.Value);
        }

        return await _testSuiteExecutor.Execute(jTestSuites);
    }

    private IEnumerable<JTestSuite> ReadTestSuites(RunCommandSettings settings)
    {
        var testFiles = JsonFileSearcher.Search(settings.TestFilePatterns, settings.GetCategories());

        return testFiles.Select(filePath =>
        {
            var json = File.ReadAllText(filePath);
            var testSuite = JsonSerializer.Deserialize<JTestSuite>(json, _serializerOptionsCache.Options)
                ?? throw new ArgumentException($"Test suite at path '{filePath}' is not a valid JTestSuite");
            testSuite.FilePath = filePath;

            return testSuite;
        });
    }

    private void InitializeVariablesContext(RunCommandSettings settings)
    {
        var environmentVariables = settings.GetEnvironmentVariables();
        var globalVariables = settings.GetGlobalVariables();
        _variablesContext.Initialize(environmentVariables, globalVariables);
    }

    private static string GetOutputDirectory(RunCommandSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputDirectoryPath))
            return settings.OutputDirectoryPath;

        return Directory.GetCurrentDirectory();
    }
}
