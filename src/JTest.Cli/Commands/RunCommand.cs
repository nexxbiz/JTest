using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Reporting;
using JTest.Core.Reporting.Html;
using JTest.Core.Templates;
using JTest.Core.Tracing;
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

        var startedAt = DateTimeOffset.UtcNow;
        var results = await ExecuteRunCommand(settings, cancellationToken);
        var endedAt = DateTimeOffset.UtcNow;

        if (results is null)
        {
            // No test files matched — a run that produced nothing is not success (FR-003).
            return (int)RunExitCode.ExecutionError;
        }

        var resultList = results as IReadOnlyList<JTestSuiteExecutionResult> ?? results.ToList();

        _testExecutionResultsProcessor.WriteConsoleSummary(resultList);

        // The canonical trace is the single source of truth for both the exit code and the reports:
        // its aggregated counts yield the class-specific code (test failure / execution error /
        // validation / aborted), so a timeout or cancellation is exit 4, never a false green.
        var toolVersion = typeof(RunCommand).Assembly.GetName().Version?.ToString(3) ?? "2.0.0";
        var trace = ExecutionTraceAssembler.Assemble(resultList, toolVersion, 0, startedAt, endedAt);

        var emptyDiscovery = !resultList.Any(r => r.Errored)
            && resultList.Sum(r => r.CasesPassed + r.CasesFailed) == 0;
        var exitCode = ExitCodeService.From(trace.Counts, emptyDiscovery: emptyDiscovery);

        var environment = settings.IncludeVariables
            ? VariableDump.Build(_variablesContext.EnvironmentVariables, _variablesContext.GlobalVariables)
            : null;

        WriteOutputs(settings, trace with { ExitCode = exitCode, Environment = environment });
        return exitCode;
    }

    // Default output folder for reports/trace when no explicit path is given.
    private const string DefaultOutputDirectory = "artifacts";
    private const string DefaultReportBaseName = "report";
    private const string DefaultTraceFileName = "trace.json";

    /// <summary>
    /// Persists the report and canonical trace, which are projections of the in-memory trace.
    /// - The default report is a self-contained HTML file; `-f markdown` produces a Markdown report instead.
    /// - With no explicit paths, both land in <c>artifacts/</c> (report.html|report.md, trace.json).
    /// - An explicit <c>--report</c>/<c>--trace</c> always wins and is written even under <c>--skip-output</c>.
    /// - <c>--skip-output</c> suppresses the defaults only.
    /// The legacy per-suite Markdown dump is gone — no report file is ever written to the suite folder.
    /// </summary>
    private static void WriteOutputs(RunCommandSettings settings, ExecutionTrace trace)
    {
        var outputDir = string.IsNullOrWhiteSpace(settings.OutputDirectoryPath)
            ? DefaultOutputDirectory
            : settings.OutputDirectoryPath;

        // A single format selector: --report-format wins for an explicit --report, else -f/--output-format.
        var format = !string.IsNullOrWhiteSpace(settings.ReportFormat) ? settings.ReportFormat : settings.OutputFormat;

        // Report: explicit --report path wins; otherwise default into the output dir unless output is skipped.
        var reportPath = settings.ReportFile;
        if (string.IsNullOrWhiteSpace(reportPath) && settings.SkipOutput != true)
        {
            var ext = IsMarkdown(format, path: null) ? ".md" : ".html";
            reportPath = Path.Combine(outputDir, DefaultReportBaseName + ext);
        }
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            EnsureDirectory(reportPath);
            if (IsMarkdown(format, reportPath))
                File.WriteAllText(reportPath, new JTest.Core.Reporting.Markdown.MarkdownReportGenerator().Generate(trace));
            else
                new HtmlReportGenerator().Write(trace, reportPath);
        }

        // Trace: explicit --trace path wins; otherwise default into the output dir unless output is skipped.
        var tracePath = settings.TraceFile;
        if (string.IsNullOrWhiteSpace(tracePath) && settings.SkipOutput != true)
            tracePath = Path.Combine(outputDir, DefaultTraceFileName);
        if (!string.IsNullOrWhiteSpace(tracePath))
        {
            EnsureDirectory(tracePath);
            File.WriteAllText(tracePath, TraceJson.Serialize(trace));
        }
    }

    /// <summary>Markdown when the format is explicitly markdown/md, or (absent a format) the path ends in .md.</summary>
    private static bool IsMarkdown(string? format, string? path)
    {
        if (!string.IsNullOrWhiteSpace(format))
            return format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
                || format.Equals("md", StringComparison.OrdinalIgnoreCase);
        return path is not null && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    private async Task<IEnumerable<JTestSuiteExecutionResult>?> ExecuteRunCommand(RunCommandSettings settings, CancellationToken cancellationToken)
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
            return _testSuiteExecutor.ExecuteParallel(jTestSuites, settings.ParallelTestExecutionCount.Value, cancellationToken);
        }

        return await _testSuiteExecutor.Execute(jTestSuites, cancellationToken);
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
}
