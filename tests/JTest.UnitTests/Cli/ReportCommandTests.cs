using JTest.Cli.Commands;
using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core.Reporting.Html;
using JTest.Core.Tracing;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.UnitTests.Cli;

/// <summary>
/// End-to-end cover for <c>jtest report</c>: real trace files on disk in, real report file out.
/// The merge rules themselves are tested in <c>TraceMergerTests</c>; these tests cover the wiring
/// that could silently undo them — a repeated <c>--trace</c> that only binds once, a refusal that
/// still writes a report, or a merged trace that is not what was rendered.
/// </summary>
public class ReportCommandTests : IDisposable
{
    private readonly string _dir = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"jtest-report-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void RepeatedTraceOption_BindsEveryOccurrence()
    {
        // Spectre only binds a repeated option to an ARRAY property (the #74 failure mode). Without
        // this the second --trace is dropped and the "merged" report is silently one invocation.
        var a = WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }));
        var b = WriteTrace("b.json", Trace("s-b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }));

        var settings = ParseSettings("--trace", a, "--trace", b, "-o", "out.html");

        Assert.Equal(new[] { a, b }, settings.TraceFiles);
        Assert.Equal("out.html", settings.OutputFile);
    }

    [Fact]
    public async Task TwoTraces_RenderOneReportWithBothSuitesAndSummedCounts()
    {
        var a = WriteTrace("part1.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 8, Passed = 8 }));
        var b = WriteTrace("part2.json", Trace("s-b", Outcome.Failed, 1, new Rollup { Total = 5, Passed = 4, Failed = 1 }));
        var report = Path.Combine(_dir, "merged.html");
        var mergedTrace = Path.Combine(_dir, "merged.json");

        var exit = await Run("--trace", a, "--trace", b, "-o", report, "--merged-trace", mergedTrace);

        Assert.Equal(0, exit);

        var merged = TraceJson.Deserialize(File.ReadAllText(mergedTrace))!;
        Assert.Equal(13, merged.Counts.Total);
        Assert.Equal(Outcome.Failed, merged.Outcome);
        Assert.Equal(1, merged.ExitCode);
        Assert.Equal(new[] { "input[0]/s-a", "input[1]/s-b" }, merged.Suites.Select(s => s.Id));

        // The written report is a projection of the written merged trace, not of one input.
        var html = File.ReadAllText(report);
        Assert.Contains("s-a", html);
        Assert.Contains("s-b", html);
    }

    [Fact]
    public async Task SingleTrace_ReRendersByteIdenticallyToRenderingItDirectly()
    {
        var trace = Trace("only", Outcome.Failed, 1, new Rollup { Total = 2, Passed = 1, Failed = 1 }) with
        {
            Run = new Dictionary<string, object?> { ["uuid"] = "keep-me" }
        };
        var path = WriteTrace("one.json", trace);
        var report = Path.Combine(_dir, "one.html");

        var exit = await Run("--trace", path, "-o", report);

        Assert.Equal(0, exit);

        // Re-rendering must add nothing: no merge note, no dropped $.run.
        var expected = new HtmlReportGenerator().Generate(TraceJson.Deserialize(File.ReadAllText(path))!);
        Assert.Equal(expected, File.ReadAllText(report));
        Assert.Contains("keep-me", File.ReadAllText(report));
    }

    [Fact]
    public async Task SchemaVersionMismatch_ExitsTwoAndWritesNoReport()
    {
        var a = WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }));
        var b = WriteTrace("b.json", Trace("s-b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 })
            with { TraceSchemaVersion = "2.0.0" });
        var report = Path.Combine(_dir, "merged.html");

        var exit = await Run("--trace", a, "--trace", b, "-o", report);

        Assert.Equal(2, exit);
        Assert.False(File.Exists(report));
    }

    [Fact]
    public async Task TwoRunsWithTheSamePositionalSuiteId_BothAppearInTheReport()
    {
        // What a real catalog produces: every `jtest run` numbers its suites from zero, so both
        // traces carry "suite[0]". Neither may be dropped, and neither may overwrite the other.
        var a = WriteTrace("a.json", Trace("suite[0]", Outcome.Passed, 0, new Rollup { Total = 8, Passed = 8 }));
        var b = WriteTrace("b.json", Trace("suite[0]", Outcome.Failed, 1, new Rollup { Total = 5, Passed = 4, Failed = 1 }));
        var report = Path.Combine(_dir, "merged.html");
        var mergedTrace = Path.Combine(_dir, "merged.json");

        var exit = await Run("--trace", a, "--trace", b, "-o", report, "--merged-trace", mergedTrace);

        Assert.Equal(0, exit);

        var merged = TraceJson.Deserialize(File.ReadAllText(mergedTrace))!;
        Assert.Equal(2, merged.Suites.Count);
        Assert.Equal(13, merged.Counts.Total);
        Assert.Equal(new[] { "input[0]/suite[0]", "input[1]/suite[0]" }, merged.Suites.Select(s => s.Id));
    }

    [Fact]
    public async Task MalformedTrace_ExitsTwoAndWritesNoReport()
    {
        var path = Path.Combine(_dir, "broken.json");
        File.WriteAllText(path, "{ not json");
        var report = Path.Combine(_dir, "out.html");

        var exit = await Run("--trace", path, "-o", report);

        Assert.Equal(2, exit);
        Assert.False(File.Exists(report));
    }

    [Fact]
    public async Task OneMalformedTraceAmongSeveral_IsRefusedRatherThanSkipped()
    {
        // A trace that cannot be read must stop the merge. Skipping it would produce a report that
        // looks complete and is quietly missing a whole invocation's results.
        var good = WriteTrace("good.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 8, Passed = 8 }));
        var broken = Path.Combine(_dir, "broken.json");
        File.WriteAllText(broken, "{ not json");
        var report = Path.Combine(_dir, "merged.html");

        var exit = await Run("--trace", good, "--trace", broken, "-o", report);

        Assert.Equal(2, exit);
        Assert.False(File.Exists(report));
    }

    [Fact]
    public async Task MarkdownFormat_WritesMarkdownCarryingTheMergeNote()
    {
        var a = WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }));
        var b = WriteTrace("b.json", Trace("s-b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }));
        var report = Path.Combine(_dir, "merged.md");

        var exit = await Run("--trace", a, "--trace", b, "-o", report, "--format", "markdown");

        Assert.Equal(0, exit);

        var md = File.ReadAllText(report);
        Assert.StartsWith("# JTest Report", md);

        // The merged-run breakdown must survive into whichever format was asked for — a reader of
        // the Markdown report cannot tell a summed duration from a wall-clock span either.
        Assert.Contains("## Merged from 2 runs", md);
        Assert.Contains("spent testing", md);
        Assert.Contains("between runs", md);
        Assert.Contains("| run 1 |", md);
        Assert.Contains("| run 2 |", md);

        // Durations must not carry the rendering machine's decimal separator: a report is read
        // elsewhere, and a CI agent's locale must not change the artifact.
        Assert.DoesNotContain(",0 s", md);
        Assert.DoesNotContain(",7 s", md);
    }

    [Fact]
    public async Task MergedReportRenders_TheSameNumbers_UnderANonInvariantLocale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL"); // comma as the decimal separator
        try
        {
            // Durations chosen to have a fractional second — that is the only place a decimal
            // separator can appear, so a whole number of seconds would not test anything.
            var a = WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, durationMs: 15_250));
            var b = WriteTrace("b.json", Trace("s-b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, durationMs: 15_250));
            var report = Path.Combine(_dir, "loc.md");

            Assert.Equal(0, await Run("--trace", a, "--trace", b, "-o", report, "--format", "markdown"));

            var md = File.ReadAllText(report);
            Assert.Contains("30.5 s", md);        // summed to 30_500 ms, rendered invariantly
            Assert.DoesNotContain("30,5 s", md);  // not the nl-NL separator
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void UnknownFormat_IsAValidationError()
    {
        var settings = new ReportCommandSettings
        {
            TraceFiles = [WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }))],
            OutputFile = "out.html",
            Format = "pdf"
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
        Assert.Contains("pdf", result.Message);
    }

    [Fact]
    public void MissingTraceFile_IsAValidationError()
    {
        var settings = new ReportCommandSettings
        {
            TraceFiles = [Path.Combine(_dir, "does-not-exist.json")],
            OutputFile = "out.html"
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
        Assert.Contains("cannot be found", result.Message);
    }

    [Fact]
    public void NoTraceOption_IsAValidationError()
    {
        var result = new ReportCommandSettings { OutputFile = "out.html" }.Validate();

        Assert.False(result.Successful);
        Assert.Contains("--trace", result.Message);
    }

    [Fact]
    public void NoOutputOption_IsAValidationError()
    {
        var settings = new ReportCommandSettings
        {
            TraceFiles = [WriteTrace("a.json", Trace("s-a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }))]
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
        Assert.Contains("output", result.Message);
    }

    private async Task<int> Run(params string[] args)
    {
        var settings = ParseSettings(args);
        Assert.True(settings.Validate().Successful);

        // A console writing to a buffer: the command's output is not under test, but it must not
        // touch the real stdout while xunit runs these in parallel.
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        var command = new ReportCommand(console, new ErrorHandlingService(console));
        return await command.ExecuteAsync(null!, settings, CancellationToken.None);
    }

    private static ReportCommandSettings ParseSettings(params string[] args)
    {
        CaptureSettingsCommand.Captured = null;

        var app = new CommandApp<CaptureSettingsCommand>();
        app.Configure(config => config.PropagateExceptions());

        Assert.Equal(0, app.Run(args));
        Assert.NotNull(CaptureSettingsCommand.Captured);

        return CaptureSettingsCommand.Captured;
    }

    private string WriteTrace(string fileName, ExecutionTrace trace)
    {
        var path = Path.Combine(_dir, fileName);
        File.WriteAllText(path, TraceJson.Serialize(trace));
        return path;
    }

    private static ExecutionTrace Trace(string suiteId, Outcome outcome, int exitCode, Rollup counts, double durationMs = 30_000)
    {
        var startedAt = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
        var suite = new SuiteResult
        {
            Id = suiteId,
            Path = suiteId,
            Name = suiteId,
            Outcome = outcome,
            Counts = counts
        };

        return new ExecutionTrace
        {
            ToolVersion = "2.0.0",
            StartedAt = startedAt,
            EndedAt = startedAt.AddMilliseconds(durationMs),
            DurationMs = durationMs,
            Outcome = outcome,
            ExitCode = exitCode,
            Counts = counts,
            Suites = [suite]
        };
    }

    private sealed class CaptureSettingsCommand : Command<ReportCommandSettings>
    {
        public static ReportCommandSettings? Captured;

        public override int Execute(CommandContext context, ReportCommandSettings settings, CancellationToken cancellationToken)
        {
            Captured = settings;
            return 0;
        }
    }
}
