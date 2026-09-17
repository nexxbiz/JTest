using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core.Execution;
using JTest.Core.Reporting.Html;
using JTest.Core.Reporting.Markdown;
using JTest.Core.Tracing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

/// <summary>
/// Renders a report from saved execution traces, merging them when more than one is given.
///
/// A suite catalog that cannot be a single <c>jtest run</c> — because the system under test must be
/// restarted part-way through — produces one trace per invocation, and therefore one report per
/// invocation. This command collapses those into one report and one merged trace, so a CI artifact
/// holds a single document and a single answer. Merge semantics live in <see cref="TraceMerger"/>.
///
/// The command is a pure projection: it reads traces and writes files. It never executes tests, and
/// with a single <c>--trace</c> it re-renders that trace unchanged.
/// </summary>
public sealed class ReportCommand(IAnsiConsole ansiConsole, IErrorHandlingService errorHandlingService)
    : CommandBase<ReportCommandSettings>(ansiConsole, errorHandlingService)
{
    public override Task<int> ExecuteAsync(
        CommandContext context, ReportCommandSettings settings, CancellationToken cancellationToken)
    {
        var inputs = new List<TraceMergeInput>(settings.TraceFiles!.Length);

        foreach (var path in settings.TraceFiles!)
        {
            var trace = ReadTrace(path);
            if (trace is null) return Task.FromResult((int)RunExitCode.ExecutionError);
            inputs.Add(new TraceMergeInput(path, trace));
        }

        ExecutionTrace merged;
        try
        {
            merged = TraceMerger.Merge(inputs);
        }
        catch (TraceMergeException ex)
        {
            // Refusing is the point: a merged report that guesses is worse than no merged report.
            Console.WriteLine($"Error: {ex.Message}", new Style(foreground: Color.Red));
            return Task.FromResult((int)RunExitCode.ExecutionError);
        }

        WriteReport(settings, merged);

        if (!string.IsNullOrWhiteSpace(settings.MergedTraceFile))
        {
            EnsureDirectory(settings.MergedTraceFile);
            File.WriteAllText(settings.MergedTraceFile, TraceJson.Serialize(merged));
        }

        WriteSummary(settings, merged, inputs.Count);

        return Task.FromResult((int)RunExitCode.Success);
    }

    private ExecutionTrace? ReadTrace(string path)
    {
        try
        {
            var trace = TraceJson.Deserialize(File.ReadAllText(path));
            if (trace is not null) return trace;

            Console.WriteLine($"Error: Trace file '{path}' is empty or not an execution trace.",
                new Style(foreground: Color.Red));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Trace file '{path}' could not be read: {ex.Message}",
                new Style(foreground: Color.Red));
        }

        return null;
    }

    private static void WriteReport(ReportCommandSettings settings, ExecutionTrace trace)
    {
        var path = settings.OutputFile!;
        EnsureDirectory(path);

        if (settings.WantsMarkdown())
            File.WriteAllText(path, new MarkdownReportGenerator().Generate(trace));
        else
            new HtmlReportGenerator().Write(trace, path);
    }

    /// <summary>
    /// Prints the single number the merge exists to produce. The process exit code stays the render
    /// status (0 / 2) so "the report failed to build" is never confused with "the tests failed" —
    /// the catalog's own result is in the summary, the report, and the merged trace's <c>exitCode</c>.
    /// </summary>
    private void WriteSummary(ReportCommandSettings settings, ExecutionTrace trace, int inputCount)
    {
        var counts = trace.Counts;
        Console.WriteLine(
            $"Rendered {settings.OutputFile} from {inputCount} trace(s): outcome {trace.Outcome}, " +
            $"exit {trace.ExitCode}, {counts.Total} result(s) " +
            $"({counts.Passed} passed, {counts.Failed} failed, {counts.Errored} errored, " +
            $"{counts.Cancelled} cancelled, {counts.TimedOut} timed out, {counts.Skipped} skipped).");
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
