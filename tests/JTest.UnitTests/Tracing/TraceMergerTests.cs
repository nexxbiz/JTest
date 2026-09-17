using System.Text.Json;
using Json.Schema;
using JTest.Core.Reporting.Html;
using JTest.Core.Tracing;
using JTest.UnitTests.Reporting;
using Xunit;

namespace JTest.UnitTests.Tracing;

/// <summary>
/// The merge semantics behind <c>jtest report --trace a.json --trace b.json</c>. Each test targets a
/// way the merge could look right and be wrong: counts that came from one input, an outcome decided
/// by input order, two shapes merged into one, two suites claiming one identity, a duration that
/// silently bills the gap between invocations, and a single-trace re-render that quietly rewrites.
/// </summary>
public class TraceMergerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TwoTraces_SumCountsElementWise()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 8, Passed = 7, Skipped = 1 }, T0, T0.AddSeconds(40), 40_000);
        var b = Trace("b", Outcome.Failed, 1, new Rollup { Total = 5, Passed = 3, Failed = 1, TimedOut = 1 }, T0.AddMinutes(4), T0.AddMinutes(4).AddSeconds(34), 34_000);

        var merged = Merge(a, b);

        Assert.Equal(13, merged.Counts.Total);
        Assert.Equal(10, merged.Counts.Passed);
        Assert.Equal(1, merged.Counts.Failed);
        Assert.Equal(0, merged.Counts.Errored);
        Assert.Equal(1, merged.Counts.TimedOut);
        Assert.Equal(1, merged.Counts.Skipped);
        Assert.Equal(0, merged.Counts.Cancelled);
    }

    [Fact]
    public void FailedInput_MakesMergedOutcomeFailed_EvenWhenListedFirst()
    {
        // Order must not decide the verdict: the failing trace is deliberately the FIRST input here
        // and the SECOND in the next case. Both must report failed.
        var failed = Trace("fail", Outcome.Failed, 1, new Rollup { Total = 1, Failed = 1 }, T0, T0.AddSeconds(1), 1_000);
        var passed = Trace("pass", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0.AddSeconds(2), T0.AddSeconds(3), 1_000);

        Assert.Equal(Outcome.Failed, Merge(failed, passed).Outcome);
        Assert.Equal(1, Merge(failed, passed).ExitCode);

        Assert.Equal(Outcome.Failed, Merge(passed, failed).Outcome);
        Assert.Equal(1, Merge(passed, failed).ExitCode);
    }

    [Fact]
    public void ErroredInput_OutranksFailedInput_UnderDocumentedPrecedence()
    {
        // errored > timedOut > cancelled > failed > passed, and exit-code class 2 > 3 > 4 > 1.
        var failed = Trace("fail", Outcome.Failed, 1, new Rollup { Total = 1, Failed = 1 }, T0, T0.AddSeconds(1), 1_000);
        var errored = Trace("err", Outcome.Errored, 2, new Rollup { Total = 1, Errored = 1 }, T0, T0.AddSeconds(1), 1_000);
        var timedOut = Trace("to", Outcome.TimedOut, 4, new Rollup { Total = 1, TimedOut = 1 }, T0, T0.AddSeconds(1), 1_000);

        Assert.Equal(Outcome.Errored, Merge(failed, errored).Outcome);
        Assert.Equal(2, Merge(failed, errored).ExitCode);

        Assert.Equal(Outcome.TimedOut, Merge(failed, timedOut).Outcome);
        Assert.Equal(4, Merge(failed, timedOut).ExitCode);

        Assert.Equal(Outcome.Errored, Merge(timedOut, errored).Outcome);
        Assert.Equal(2, Merge(timedOut, errored).ExitCode);
    }

    [Fact]
    public void AllPassing_MergesToPassedAndExitZero()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 2, Passed = 2 }, T0, T0.AddSeconds(1), 1_000);
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 3, Passed = 3 }, T0.AddSeconds(2), T0.AddSeconds(3), 1_000);

        var merged = Merge(a, b);

        Assert.Equal(Outcome.Passed, merged.Outcome);
        Assert.Equal(0, merged.ExitCode);
    }

    [Fact]
    public void MajorSchemaVersionMismatch_IsRefused()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000);
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000)
            with { TraceSchemaVersion = "2.0.0" };

        var ex = Assert.Throws<TraceMergeException>(() => Merge(a, b));

        Assert.Contains("traceSchemaVersion", ex.Message);
        Assert.Contains("1.0.0", ex.Message);
        Assert.Contains("2.0.0", ex.Message);
    }

    [Fact]
    public void MinorSchemaVersionDifference_Merges_SoAMergedTraceCanBeRemerged()
    {
        // A merged trace declares 1.1.0 (it carries the merge block). Refusing any mismatch would
        // make it impossible to merge that result with a fresh 1.0.0 run — minor versions are
        // additive, so they describe the same shape.
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-a");
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-b")
            with { TraceSchemaVersion = "1.1.0" };

        var merged = Merge(a, b);

        Assert.Equal(TraceMerger.MergedSchemaVersion, merged.TraceSchemaVersion);
        Assert.Equal(2, merged.Suites.Count);
    }

    [Fact]
    public void CollidingPositionalSuiteIds_AreNamespacedByInput_NotRefusedAndNotCollapsed()
    {
        // Suite ids are POSITIONAL within a run (TraceBuilder.Path), so the first suite of every
        // invocation is "suite[0]". Two invocations therefore always collide. Refusing would reject
        // every real merge; keeping both unchanged would put two rows with one identity in the
        // report. Both suites must survive, each under its own input namespace.
        var a = Trace("part1.json", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "suite[0]");
        var b = Trace("part2.json", Outcome.Failed, 1, new Rollup { Total = 1, Failed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "suite[0]");

        var merged = Merge(a, b);

        Assert.Equal(2, merged.Suites.Count);
        Assert.Equal(new[] { "input[0]/suite[0]", "input[1]/suite[0]" }, merged.Suites.Select(s => s.Id));
        Assert.Equal(new[] { "input[0]/suite[0]", "input[1]/suite[0]" }, merged.Suites.Select(s => s.Path));
        Assert.Equal(merged.Suites.Count, merged.Suites.Select(s => s.Id).Distinct().Count());

        // The namespace is recorded, so a reader can map a row back to the run it came from.
        Assert.Equal(new[] { "input[0]", "input[1]" }, merged.Merge!.Sources.Select(s => s.IdPrefix));
        Assert.Equal(new[] { "part1.json", "part2.json" }, merged.Merge!.Sources.Select(s => s.Source));
    }

    [Fact]
    public void Namespacing_RewritesEveryNestedIdAndPath()
    {
        // A prefix on the suite alone would leave case/dataset/step/iteration/assertion ids colliding
        // across inputs — the same failure one level down.
        var a = TraceFixtures.Mixed();
        var b = TraceFixtures.Mixed();

        var merged = TraceMerger.Merge(
        [
            new TraceMergeInput("a.json", a),
            new TraceMergeInput("b.json", b)
        ]);

        var nodes = Collect(merged).ToArray();

        Assert.All(nodes, n =>
        {
            Assert.StartsWith("input[", n.Id);
            Assert.StartsWith("input[", n.Path);
        });

        // Ids must stay unique across the whole merged document, not just at suite level.
        Assert.Equal(nodes.Length, nodes.Select(n => n.Id).Distinct().Count());

        // Same tree twice: exactly half the nodes under each input, and none lost.
        Assert.Equal(nodes.Length / 2, nodes.Count(n => n.Id.StartsWith("input[0]/")));
        Assert.Equal(nodes.Length / 2, nodes.Count(n => n.Id.StartsWith("input[1]/")));
        Assert.Equal(Collect(a).Count(), nodes.Length / 2);
    }

    /// <summary>Every identified node in the trace: suites, cases, datasets, steps (and their
    /// children, iterations and assertions). An assertion has no path, so it reports its id as one.</summary>
    private static IEnumerable<(string Id, string Path)> Collect(ExecutionTrace trace)
    {
        foreach (var suite in trace.Suites)
        {
            yield return (suite.Id, suite.Path);
            foreach (var @case in suite.Cases)
            {
                yield return (@case.Id, @case.Path);
                foreach (var dataset in @case.Datasets)
                {
                    yield return (dataset.Id, dataset.Path);
                    foreach (var node in dataset.Steps.SelectMany(Collect)) yield return node;
                }
            }
        }
    }

    private static IEnumerable<(string Id, string Path)> Collect(StepNode step)
    {
        yield return (step.Id, step.Path);
        foreach (var assertion in step.Assertions ?? []) yield return (assertion.Id, assertion.Id);
        foreach (var node in (step.Children ?? []).SelectMany(Collect)) yield return node;
        foreach (var iteration in step.Iterations ?? [])
        {
            yield return (iteration.Id, iteration.Path);
            foreach (var node in iteration.Steps.SelectMany(Collect)) yield return node;
        }
    }

    [Fact]
    public void Suites_ConcatenateInInputOrder()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "first");
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "second");

        var merged = Merge(a, b);

        Assert.Equal(new[] { "input[0]/first", "input[1]/second" }, merged.Suites.Select(s => s.Id));
        Assert.Equal(new[] { "a", "b" }, merged.Suites.Select(s => s.Name));
    }

    [Fact]
    public void MergedDuration_IsTheSumAndExcludesTheGapBetweenInputs()
    {
        // The consumer's shape: 40s of suites, a ~4-minute engine restart, then 34s of suites.
        // The honest total is 74s. endedAt - startedAt would be ~314s — minutes of "testing" that
        // never happened.
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 8, Passed = 8 }, T0, T0.AddSeconds(40), 40_000, suiteId: "s-a");
        var gapStart = T0.AddSeconds(40 + 240);
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 5, Passed = 5 }, gapStart, gapStart.AddSeconds(34), 34_000, suiteId: "s-b");

        var merged = Merge(a, b);

        Assert.Equal(74_000d, merged.DurationMs);

        // The window is still the true outer span, so the gap is visible rather than erased...
        Assert.Equal(T0, merged.StartedAt);
        Assert.Equal(gapStart.AddSeconds(34), merged.EndedAt);
        Assert.Equal(314_000d, (merged.EndedAt - merged.StartedAt).TotalMilliseconds);

        // ...and the trace states which of the two numbers durationMs is, structurally, because the
        // two are indistinguishable to a reader and unreadable to a pipeline as prose.
        var merge = Assert.IsType<MergeInfo>(merged.Merge);
        Assert.Equal("sumOfSources", merge.DurationSemantics);
        Assert.Equal(74_000d, merge.TestDurationMs);
        Assert.Equal(314_000d, merge.ElapsedMs);
        Assert.Equal(240_000d, merge.BetweenRunsMs);
        Assert.Equal(merged.DurationMs, merge.TestDurationMs);

        // The gap is attributed to the run it preceded, so a reader sees where the time went.
        Assert.Null(merge.Sources[0].GapBeforeMs);
        Assert.Equal(240_000d, merge.Sources[1].GapBeforeMs);
    }

    [Fact]
    public void DurationFallsBackToAnInputsOwnSpanWhenItRecordedNoDuration()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(10), 10_000, suiteId: "s-a")
            with { DurationMs = null };
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0.AddMinutes(5), T0.AddMinutes(5).AddSeconds(4), 4_000, suiteId: "s-b");

        // 10s (from a's own span) + 4s — still not the ~5m14s wall clock.
        Assert.Equal(14_000d, Merge(a, b).DurationMs);
    }

    [Fact]
    public void PerInvocationRunAndEnvironment_LeaveTheRootButStayWithTheirRun()
    {
        var a = Trace("a.json", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-a")
            with
        {
            Run = new Dictionary<string, object?> { ["uuid"] = "aaaa" },
            Environment = new Dictionary<string, object?> { ["baseUrl"] = "https://a.test" }
        };
        var b = Trace("b.json", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-b")
            with
        {
            Run = new Dictionary<string, object?> { ["uuid"] = "bbbb" },
            Environment = new Dictionary<string, object?> { ["baseUrl"] = "https://b.test" }
        };

        var merged = Merge(a, b);

        // Neither invocation's values may be presented as the whole run's...
        Assert.Null(merged.Run);
        Assert.Null(merged.Environment);

        // ...but neither may be lost: each stays attributed to the run that produced it.
        Assert.Equal("aaaa", merged.Merge!.Sources[0].Run!["uuid"]);
        Assert.Equal("bbbb", merged.Merge!.Sources[1].Run!["uuid"]);
        Assert.Equal("https://a.test", merged.Merge!.Sources[0].Environment!["baseUrl"]);
        Assert.Equal("https://b.test", merged.Merge!.Sources[1].Environment!["baseUrl"]);
        Assert.Equal(new[] { "a.json", "b.json" }, merged.Merge!.Sources.Select(s => s.Source));
    }

    [Fact]
    public void InputRootDiagnostics_AreCarriedIntoTheMerge()
    {
        var a = Trace("a", Outcome.Errored, 2, new Rollup { Total = 1, Errored = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-a")
            with { Diagnostics = new[] { Diagnostic.Error("suite.json failed to load") } };
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-b");

        var merged = Merge(a, b);

        Assert.Contains(merged.Diagnostics!, d => d.Message == "suite.json failed to load");
    }

    [Fact]
    public void DifferingToolVersions_AreListedAndWarned()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-a");
        var b = Trace("b", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-b")
            with { ToolVersion = "2.1.0" };

        var merged = Merge(a, b);

        Assert.Equal("2.0.0, 2.1.0", merged.ToolVersion);
        Assert.Contains(merged.Diagnostics!, d =>
            d.Severity == DiagnosticSeverity.Warning && d.Message.Contains("different JTest versions"));
    }

    [Fact]
    public void SingleTraceMerge_IsByteIdenticalToRenderingThatTrace()
    {
        var one = TraceFixtures.Mixed() with
        {
            Run = new Dictionary<string, object?> { ["uuid"] = "keep-me" },
            Diagnostics = new[] { Diagnostic.Error("a load failure that must survive") }
        };

        var merged = TraceMerger.Merge(new[] { new TraceMergeInput("only.json", one) });

        // Same object, same JSON, and — the thing a consumer actually sees — the same HTML bytes.
        // In particular no input[0]/ namespace: a re-render must not rewrite a single trace's ids.
        Assert.Same(one, merged);
        Assert.Equal(TraceJson.Serialize(one), TraceJson.Serialize(merged));

        var generator = new HtmlReportGenerator();
        Assert.Equal(generator.Generate(one), generator.Generate(merged));
    }

    [Fact]
    public void MergedTrace_ValidatesAgainstTheTraceSchema()
    {
        var a = Trace("a", Outcome.Passed, 0, new Rollup { Total = 1, Passed = 1 }, T0, T0.AddSeconds(1), 1_000, suiteId: "s-a");
        var b = Trace("b", Outcome.Failed, 1, new Rollup { Total = 1, Failed = 1 }, T0.AddSeconds(5), T0.AddSeconds(6), 1_000, suiteId: "s-b");

        var json = TraceJson.Serialize(Merge(a, b));
        using var doc = JsonDocument.Parse(json);

        var result = TraceSchemaFixture.Schema.Evaluate(
            doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, json);
    }

    [Fact]
    public void NoInputs_IsRefused()
    {
        Assert.Throws<TraceMergeException>(() => TraceMerger.Merge(Array.Empty<TraceMergeInput>()));
    }

    private static ExecutionTrace Merge(params ExecutionTrace[] traces) =>
        TraceMerger.Merge(traces.Select(t => new TraceMergeInput(t.Suites[0].Name, t)).ToArray());

    /// <summary>A minimal but schema-valid trace with one suite, named after <paramref name="source"/>.</summary>
    private static ExecutionTrace Trace(
        string source, Outcome outcome, int exitCode, Rollup counts,
        DateTimeOffset startedAt, DateTimeOffset endedAt, double durationMs, string? suiteId = null)
    {
        var suite = new SuiteResult
        {
            Id = suiteId ?? source,
            Path = suiteId ?? source,
            Name = source,
            FilePath = source + ".json",
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMs = durationMs,
            Outcome = outcome,
            Counts = counts
        };

        return new ExecutionTrace
        {
            ToolVersion = "2.0.0",
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMs = durationMs,
            Outcome = outcome,
            ExitCode = exitCode,
            Counts = counts,
            Suites = new[] { suite }
        };
    }
}
