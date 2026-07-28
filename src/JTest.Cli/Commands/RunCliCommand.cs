using System.Globalization;
using System.Text.Json;
using JTest.Cli.Invocation;
using JTest.Cli.Loading;
using JTest.Cli.Ports;
using JTest.Engine.Execution;
using JTest.Engine.Tracing;
using JTest.Language.Diagnostics;
using JTest.Language.Semantics;
using JTest.Reporting.Canonical;
using JTest.Reporting.Writers;

namespace JTest.Cli.Commands;

/// <summary>
/// Executes suites and writes truthful reports. The exit code is computed
/// from the canonical evidence: there is no path to success without a
/// complete passing trace for every discovered suite.
/// </summary>
public sealed class RunCliCommand
{
    private readonly SuiteBundleLoader loader;
    private readonly SuiteRunner runner;
    private readonly IResultDocumentWriter resultWriter;
    private readonly ICatalogReportWriter catalogWriter;
    private readonly IStandaloneReportWriter standaloneWriter;
    private readonly IConsoleWriter console;
    private readonly IReportOpener opener;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Suite loader.</param>
    /// <param name="runner">Execution engine.</param>
    /// <param name="resultWriter">Canonical evidence writer.</param>
    /// <param name="catalogWriter">Catalog report writer.</param>
    /// <param name="standaloneWriter">Standalone report writer.</param>
    /// <param name="console">Console output.</param>
    /// <param name="opener">Best-effort report opener.</param>
    public RunCliCommand(
        SuiteBundleLoader loader,
        SuiteRunner runner,
        IResultDocumentWriter resultWriter,
        ICatalogReportWriter catalogWriter,
        IStandaloneReportWriter standaloneWriter,
        IConsoleWriter console,
        IReportOpener opener)
    {
        this.loader = loader;
        this.runner = runner;
        this.resultWriter = resultWriter;
        this.catalogWriter = catalogWriter;
        this.standaloneWriter = standaloneWriter;
        this.console = console;
        this.opener = opener;
    }

    /// <summary>Executes the run.</summary>
    /// <param name="invocation">The parsed invocation.</param>
    /// <param name="environment">Ambient session facts.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    public async Task<int> Execute(
        CliInvocation invocation,
        CliEnvironment environment,
        CancellationToken cancellationToken)
    {
        var format = invocation.LastValue("diagnostics") ?? "text";
        var files = SuiteFileDiscovery.Resolve(environment.WorkingDirectory, invocation.Arguments);
        if (files.Count == 0)
        {
            console.ErrorLine($"No suite files matched: {string.Join(", ", invocation.Arguments)}");
            return CliExitCodes.InvalidInput;
        }

        var bundles = new List<SuiteBundle>();
        var loadDiagnostics = new List<LanguageDiagnostic>();
        foreach (var file in files)
        {
            var loaded = loader.Load(file);
            loadDiagnostics.AddRange(loaded.Diagnostics);
            if (loaded.Bundle is not null && loaded.IsValid)
            {
                bundles.Add(loaded.Bundle);
            }
        }

        if (bundles.Count != files.Count)
        {
            DiagnosticsPrinter.Print(loadDiagnostics, format, console);
            console.ErrorLine("Validation failed; nothing was executed.");
            return CliExitCodes.InvalidInput;
        }

        if (!Console.RunInputBinder.TryBind(invocation, environment, out var options, out var optionError))
        {
            console.ErrorLine(optionError);
            return CliExitCodes.InvalidInput;
        }

        using var timeout = CreateTimeout(invocation, cancellationToken, out var runToken);
        var run = await runner.ExecuteRun(bundles, options, runToken).ConfigureAwait(false);
        var document = resultWriter.Write(run);

        var reportResult = WriteReports(invocation, environment, document, out var standaloneMode);
        PrintSummary(run, document);

        var reportUrl = new Uri(reportResult.IndexHtmlPath).AbsoluteUri;
        console.Out($"Report: {reportUrl}");
        console.Out($"Evidence: {reportResult.ResultJsonPath}");

        if (!standaloneMode &&
            OpenBehavior.ShouldOpen(invocation.HasFlag("open"), invocation.HasFlag("no-open"), environment) &&
            !opener.TryOpen(reportUrl))
        {
            console.ErrorLine($"warning: could not open the report automatically; open it manually: {reportUrl}");
        }

        return run.Outcome == TraceOutcome.Passed ? CliExitCodes.Passed : CliExitCodes.TestsFailed;
    }

    private CatalogWriteResult WriteReports(
        CliInvocation invocation,
        CliEnvironment environment,
        ResultDocument document,
        out bool standaloneMode)
    {
        standaloneMode = string.Equals(invocation.LastValue("report"), "standalone", StringComparison.Ordinal);
        if (standaloneMode)
        {
            var outDirectory = invocation.LastValue("report-out") ?? "jtest-report";
            return standaloneWriter.Write(
                document,
                Path.GetFullPath(Path.Combine(environment.WorkingDirectory, outDirectory)));
        }

        var reportDirectory = invocation.LastValue("report-dir") ?? Path.Combine(".jtest", "reports");
        return catalogWriter.Write(
            document,
            Path.GetFullPath(Path.Combine(environment.WorkingDirectory, reportDirectory)));
    }

    private void PrintSummary(TraceNode run, ResultDocument document)
    {
        var counts = RunCounts.Compute(run);
        var caseRuns = counts["caseRuns"]!;
        console.Out(string.Create(
            CultureInfo.InvariantCulture,
            $"Run {document.RunId}: {TraceJson.OutcomeName(run.Outcome)} — cases {caseRuns["total"]} " +
            $"(passed {caseRuns["passed"]}, failed {caseRuns["failed"]}, error {caseRuns["error"]}, " +
            $"skipped {caseRuns["skipped"]}, cancelled {caseRuns["cancelled"]}, timedOut {caseRuns["timedOut"]})"));
    }

    private static CancellationTokenSource? CreateTimeout(
        CliInvocation invocation,
        CancellationToken cancellationToken,
        out CancellationToken runToken)
    {
        var timeoutText = invocation.LastValue("timeout");
        if (timeoutText is null ||
            !double.TryParse(timeoutText, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutMs) ||
            timeoutMs <= 0)
        {
            runToken = cancellationToken;
            return null;
        }

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
        runToken = source.Token;
        return source;
    }
}
