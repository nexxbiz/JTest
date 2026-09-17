using Jint;
using JTest.Core.Reporting.Html;
using JTest.Core.Tracing;

namespace JTest.UnitTests.Reporting;

/// <summary>
/// The report's ordering rules, executed as the real JavaScript that ships inside every report.
///
/// Ordering is the one part of the report a merged run depends on for correctness — chronological
/// order is what makes "run 1, then the restart, then run 2" legible — and it lives in report.js,
/// so C# tests could only pin the data it consumes. These run the actual embedded script in a
/// JavaScript engine, with no DOM: report.js bootstraps only when a document exists, and exposes
/// its ordering helpers for exactly this.
/// </summary>
public class ReportOrderingJsTests
{
    /// <summary>Loads the report.js that is embedded in the shipped assembly — not a copy.</summary>
    private static Engine LoadReportScript()
    {
        var assembly = typeof(HtmlReportGenerator).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("report.js", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        var engine = new Engine();
        engine.Execute(reader.ReadToEnd());
        return engine;
    }

    /// <summary>Orders the given node paths/outcomes through the report's own sort.</summary>
    private static string[] Order(string mode, params (string Path, string Outcome)[] nodes)
    {
        var engine = LoadReportScript();
        var literal = string.Join(", ", nodes.Select(n => $"{{ path: '{n.Path}', outcome: '{n.Outcome}' }}"));

        var result = engine.Evaluate(
            $"JTestReport.setOrder('{mode}'); JTestReport.sortNodes([{literal}]).map(function (n) {{ return n.path; }}).join('|');");

        return result.AsString().Split('|', StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void ExecutionOrder_PutsRunsInTheOrderTheyHappened()
    {
        // The embedded trace is ordered failure-first, so the input here is deliberately scrambled:
        // the sort must reconstruct chronological order from the paths alone.
        var ordered = Order("execution",
            ("input[1]/suite[2]", "failed"),
            ("input[0]/suite[0]", "passed"),
            ("input[1]/suite[0]", "passed"),
            ("input[0]/suite[1]", "passed"),
            ("input[1]/suite[1]", "passed"),
            ("input[0]/suite[2]", "passed"));

        Assert.Equal(
            new[] { "input[0]/suite[0]", "input[0]/suite[1]", "input[0]/suite[2]",
                    "input[1]/suite[0]", "input[1]/suite[1]", "input[1]/suite[2]" },
            ordered);
    }

    [Fact]
    public void ExecutionOrder_ComparesIndicesNumerically_NotAsText()
    {
        // suite[10] must come after suite[9]. A string sort would put it after suite[1].
        var ordered = Order("execution",
            ("input[0]/suite[10]", "passed"),
            ("input[0]/suite[9]", "passed"),
            ("input[0]/suite[1]", "passed"),
            ("input[0]/suite[2]", "passed"));

        Assert.Equal(
            new[] { "input[0]/suite[1]", "input[0]/suite[2]", "input[0]/suite[9]", "input[0]/suite[10]" },
            ordered);
    }

    [Fact]
    public void ExecutionOrder_IgnoresOutcome()
    {
        var ordered = Order("execution",
            ("input[0]/suite[1]", "passed"),
            ("input[0]/suite[0]", "errored"),
            ("input[0]/suite[2]", "failed"));

        Assert.Equal(new[] { "input[0]/suite[0]", "input[0]/suite[1]", "input[0]/suite[2]" }, ordered);
    }

    [Fact]
    public void FailuresFirst_OrdersBySeverity_AndIsStillAvailable()
    {
        // The default for a single run, and one click away in a merged one. Severity precedence
        // matches the trace's own: errored > failed > timedOut > cancelled > passed > skipped.
        var ordered = Order("failures",
            ("input[0]/suite[0]", "passed"),
            ("input[0]/suite[1]", "failed"),
            ("input[0]/suite[2]", "skipped"),
            ("input[0]/suite[3]", "errored"),
            ("input[0]/suite[4]", "timedOut"));

        Assert.Equal(
            new[] { "input[0]/suite[3]", "input[0]/suite[1]", "input[0]/suite[4]",
                    "input[0]/suite[0]", "input[0]/suite[2]" },
            ordered);
    }

    [Fact]
    public void SwitchingOrderAndBack_ReturnsTheOriginalSequence()
    {
        var nodes = new[]
        {
            ("input[0]/suite[0]", "passed"),
            ("input[1]/suite[0]", "failed"),
            ("input[0]/suite[1]", "passed")
        };

        var chronological = new[] { "input[0]/suite[0]", "input[0]/suite[1]", "input[1]/suite[0]" };

        Assert.Equal(chronological, Order("execution", nodes));
        Assert.Equal(new[] { "input[1]/suite[0]", "input[0]/suite[0]", "input[0]/suite[1]" }, Order("failures", nodes));
        Assert.Equal(chronological, Order("execution", nodes));
    }

    [Fact]
    public void SingleRunPaths_WithoutAnInputPrefix_StillOrderChronologically()
    {
        // A re-rendered single trace has no input[] segment; ordering must not depend on one.
        var ordered = Order("execution",
            ("suite[2]", "passed"),
            ("suite[0]", "failed"),
            ("suite[1]", "passed"));

        Assert.Equal(new[] { "suite[0]", "suite[1]", "suite[2]" }, ordered);
    }

    [Fact]
    public void DeepPaths_OrderLevelByLevel()
    {
        var ordered = Order("execution",
            ("input[0]/suite[0]/case[1]/dataset[0]/step[0]", "passed"),
            ("input[0]/suite[0]/case[0]/dataset[0]/step[2]", "passed"),
            ("input[0]/suite[0]/case[0]/dataset[0]/step[10]", "passed"));

        Assert.Equal(
            new[] { "input[0]/suite[0]/case[0]/dataset[0]/step[2]",
                    "input[0]/suite[0]/case[0]/dataset[0]/step[10]",
                    "input[0]/suite[0]/case[1]/dataset[0]/step[0]" },
            ordered);
    }

    [Fact]
    public void OrderingAgreesWithTheMergedTraceItRendersFrom()
    {
        // End to end: merge two traces, then order the real suite nodes with the real script. The
        // chronological view must reproduce the merge's own concatenation order.
        var merged = TraceMerger.Merge(
        [
            new TraceMergeInput("first.json", TraceFixtures.Mixed()),
            new TraceMergeInput("second.json", TraceFixtures.Mixed())
        ]);

        var scrambled = merged.Suites.Reverse()
            .Select(s => (s.Path, Outcome: s.Outcome.ToString().ToLowerInvariant()))
            .ToArray();

        Assert.Equal(merged.Suites.Select(s => s.Path), Order("execution", scrambled));
    }
}
