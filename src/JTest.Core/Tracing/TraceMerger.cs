using JTest.Core.Execution;

namespace JTest.Core.Tracing;

/// <summary>Raised when inputs cannot be honestly merged. The merge refuses rather than guesses.</summary>
public sealed class TraceMergeException(string message) : Exception(message);

/// <summary>One trace to merge, paired with the path it was read from (used in diagnostics/errors).</summary>
public sealed record TraceMergeInput(string Source, ExecutionTrace Trace);

/// <summary>
/// Merges several canonical traces — one per <c>jtest run</c> invocation — into a single trace that a
/// report can project. A suite catalog that cannot be one invocation (e.g. the system under test is
/// restarted between two groups of suites) otherwise yields several traces, several reports, and no
/// single answer to "did the catalog pass".
///
/// The semantics are chosen deliberately, not inherited from whatever the types make easy:
///
/// <list type="bullet">
/// <item><b>Single input is identity.</b> Merging one trace returns it unchanged, so
///   <c>jtest report</c> over one trace is a pure re-render — byte-identical to rendering it directly.
///   Merge semantics engage only from two inputs up.</item>
/// <item><b><c>traceSchemaVersion</c> must be compatible.</b> Inputs must share a MAJOR version —
///   two different shapes are never merged. The merged trace declares <see cref="MergedSchemaVersion"/>,
///   which is the first version that defines the <c>merge</c> block it carries.</item>
/// <item><b><c>startedAt</c> = min, <c>endedAt</c> = max</b> — the outer wall-clock window.</item>
/// <item><b><c>durationMs</c> = the SUM of the inputs' durations</b>, NOT <c>endedAt - startedAt</c>.
///   The span silently absorbs the gap between invocations (an engine restart, a queued CI step), so a
///   74-second catalog would be reported as minutes of testing. Because the two numbers are
///   indistinguishable to a reader, the <c>merge</c> block separates them: <c>testDurationMs</c>,
///   <c>betweenRunsMs</c> and <c>elapsedMs</c> are named individually rather than explained in prose.</item>
/// <item><b><c>counts</c> sum element-wise.</b></item>
/// <item><b><c>outcome</c> is the worst of the inputs</b> under the precedence already documented for
///   parent nodes (<see cref="OutcomeExtensions.Aggregate"/>): errored &gt; timedOut &gt; cancelled &gt;
///   failed &gt; passed; <c>skipped</c> only when every input was skipped.</item>
/// <item><b><c>exitCode</c> follows the CLI's own class precedence</b> 2 &gt; 3 &gt; 4 &gt; 1 &gt; 0 —
///   strictly stronger than "non-zero if any input is non-zero", and the same ordering a single run uses.</item>
/// <item><b><c>suites</c> concatenate in input order, with every node’s id and path namespaced by
///   the input they came from</b> (<c>suite[0]</c> becomes <c>input[0]/suite[0]</c>, and its cases,
///   datasets, steps, iterations and assertions follow). Ids are positional within a run
///   (<see cref="TraceBuilder.Path"/>), so the FIRST suite of every invocation is <c>suite[0]</c> and
///   a plain concatenation would put two rows claiming the same identity in one report — not as an
///   edge case but always. Refusing the collision was the alternative; it would reject every real
///   merge, since a consumer cannot give a positional id a different value. Namespacing is applied
///   uniformly rather than only on collision, so a node’s identity does not depend on what it
///   happened to be merged with. Ids that still collide after namespacing (a hand-edited trace) are
///   refused.</item>
/// <item><b><c>run</c> and <c>environment</c> move off the merged root and onto each source.</b>
///   They are per-invocation dictionaries; picking one invocation's values and presenting them as
///   the run's would be a lie. Rather than drop them, the merge keeps each run's own values under
///   <c>merge.sources[n]</c>, where they stay attributed to the run that produced them.</item>
/// <item><b>A <c>merge</c> block records all of this in the trace.</b> The alternative was prose in
///   the diagnostics, which a reader has to parse and a pipeline cannot read at all.</item>
/// </list>
/// </summary>
public static class TraceMerger
{
    public static ExecutionTrace Merge(IReadOnlyList<TraceMergeInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw new TraceMergeException("At least one trace is required.");

        // Identity for a single input: re-rendering one trace must not be a merge in disguise.
        if (inputs.Count == 1) return inputs[0].Trace;

        RequireSameSchemaVersion(inputs);
        var suites = ConcatenateSuites(inputs);

        var startedAt = inputs.Min(i => i.Trace.StartedAt);
        var endedAt = inputs.Max(i => i.Trace.EndedAt);
        var summedMs = inputs.Sum(i => DurationOf(i.Trace));
        var wallClockMs = (endedAt - startedAt).TotalMilliseconds;

        return new ExecutionTrace
        {
            TraceSchemaVersion = MergedSchemaVersion,
            ToolVersion = MergeToolVersion(inputs),
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMs = summedMs,
            Outcome = OutcomeExtensions.Aggregate(inputs.Select(i => i.Trace.Outcome)),
            ExitCode = MergeExitCode(inputs.Select(i => i.Trace.ExitCode)),
            Counts = SumCounts(inputs.Select(i => i.Trace.Counts)),
            Suites = suites,
            Diagnostics = BuildDiagnostics(inputs),
            Merge = BuildMergeInfo(inputs, summedMs, wallClockMs)

            // Deliberately not set: Run and Environment. They live per source on Merge.Sources.
        };
    }

    /// <summary>An input's own duration, falling back to its wall-clock span when it recorded none.</summary>
    private static double DurationOf(ExecutionTrace trace) =>
        trace.DurationMs ?? (trace.EndedAt - trace.StartedAt).TotalMilliseconds;

    /// <summary>
    /// The schema version a merged trace declares: the first one that defines the <c>merge</c> block.
    /// Minor versions are additive, so a 1.0.0 reader still finds everything it knows about.
    /// </summary>
    public const string MergedSchemaVersion = "1.1.0";

    /// <summary>
    /// Inputs must share a MAJOR version. A differing MINOR is additive by definition and merges
    /// cleanly (that is what lets a merged 1.1.0 trace be re-merged with a fresh 1.0.0 run); a
    /// differing MAJOR is a different document shape and is refused rather than reconciled.
    /// </summary>
    private static void RequireSameSchemaVersion(IReadOnlyList<TraceMergeInput> inputs)
    {
        var baseline = inputs[0];
        foreach (var input in inputs.Skip(1))
        {
            if (Major(input.Trace.TraceSchemaVersion) == Major(baseline.Trace.TraceSchemaVersion)) continue;

            throw new TraceMergeException(
                "Cannot merge traces with incompatible traceSchemaVersion: " +
                $"'{baseline.Source}' is {baseline.Trace.TraceSchemaVersion} but " +
                $"'{input.Source}' is {input.Trace.TraceSchemaVersion}. " +
                "Only traces sharing a major version describe the same document shape.");
        }
    }

    private static string Major(string version)
    {
        var dot = version.IndexOf('.');
        return dot < 0 ? version : version[..dot];
    }

    /// <summary>The path segment every node from <paramref name="index"/>’th input is namespaced under.</summary>
    public static string InputSegment(int index) => TraceBuilder.Path("", "input", index);

    /// <summary>
    /// Concatenates suites in input order, re-rooting each input’s nodes under its own
    /// <c>input[n]</c> segment so positional ids from different invocations cannot collide.
    /// </summary>
    private static IReadOnlyList<SuiteResult> ConcatenateSuites(IReadOnlyList<TraceMergeInput> inputs)
    {
        var suites = new List<SuiteResult>();
        var seenIn = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            var prefix = InputSegment(index);

            foreach (var suite in input.Trace.Suites)
            {
                var namespaced = Reroot(suite, prefix);

                if (seenIn.TryGetValue(namespaced.Id, out var firstSource))
                {
                    // Unreachable for traces JTest produced; a hand-edited one must still not yield a
                    // report with two rows claiming the same identity.
                    throw new TraceMergeException(
                        $"Duplicate suite id '{namespaced.Id}' in '{firstSource}' and '{input.Source}' " +
                        "even after namespacing by input. Give the suites distinct ids and re-run.");
                }

                seenIn[namespaced.Id] = input.Source;
                suites.Add(namespaced);
            }
        }

        return suites;
    }

    // Ids are execution paths (TraceBuilder.Id), so re-rooting a subtree is a prefix on every id and
    // path in it. Nothing else in a node depends on the path, and the report treats paths as display
    // and search text only.
    private static string P(string prefix, string path) => $"{prefix}/{path}";

    private static SuiteResult Reroot(SuiteResult suite, string prefix) => suite with
    {
        Id = P(prefix, suite.Id),
        Path = P(prefix, suite.Path),
        Cases = suite.Cases.Select(c => Reroot(c, prefix)).ToArray()
    };

    private static CaseResult Reroot(CaseResult @case, string prefix) => @case with
    {
        Id = P(prefix, @case.Id),
        Path = P(prefix, @case.Path),
        Datasets = @case.Datasets.Select(d => Reroot(d, prefix)).ToArray()
    };

    private static DatasetResult Reroot(DatasetResult dataset, string prefix) => dataset with
    {
        Id = P(prefix, dataset.Id),
        Path = P(prefix, dataset.Path),
        Steps = dataset.Steps.Select(s => Reroot(s, prefix)).ToArray()
    };

    private static StepNode Reroot(StepNode step, string prefix) => step with
    {
        Id = P(prefix, step.Id),
        Path = P(prefix, step.Path),
        Children = step.Children?.Select(c => Reroot(c, prefix)).ToArray(),
        Iterations = step.Iterations?.Select(i => Reroot(i, prefix)).ToArray(),
        Assertions = step.Assertions?.Select(a => a with { Id = P(prefix, a.Id) }).ToArray()
    };

    private static Iteration Reroot(Iteration iteration, string prefix) => iteration with
    {
        Id = P(prefix, iteration.Id),
        Path = P(prefix, iteration.Path),
        Steps = iteration.Steps.Select(s => Reroot(s, prefix)).ToArray()
    };

    /// <summary>The single tool version when the inputs agree, otherwise every distinct one.</summary>
    private static string MergeToolVersion(IReadOnlyList<TraceMergeInput> inputs)
    {
        var versions = DistinctToolVersions(inputs);
        return versions.Length == 1 ? versions[0] : string.Join(", ", versions);
    }

    private static string[] DistinctToolVersions(IReadOnlyList<TraceMergeInput> inputs) =>
        inputs.Select(i => i.Trace.ToolVersion)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// The most severe input exit code under the CLI's documented class precedence
    /// (execution-error 2 &gt; validation-error 3 &gt; aborted 4 &gt; test-failure 1 &gt; success 0).
    /// Any unknown non-zero code outranks all of them rather than being quietly ignored.
    /// </summary>
    private static int MergeExitCode(IEnumerable<int> exitCodes)
    {
        var codes = exitCodes.ToArray();

        var unknown = codes.Where(c => c is < 0 or > 4).ToArray();
        if (unknown.Length > 0) return unknown.Max();

        int[] precedence =
        [
            (int)RunExitCode.ExecutionError,
            (int)RunExitCode.ValidationError,
            (int)RunExitCode.Aborted,
            (int)RunExitCode.TestFailure
        ];

        foreach (var candidate in precedence)
        {
            if (codes.Contains(candidate)) return candidate;
        }

        return (int)RunExitCode.Success;
    }

    private static Rollup SumCounts(IEnumerable<Rollup> counts)
    {
        var sum = Rollup.Empty;
        foreach (var c in counts)
        {
            sum = new Rollup
            {
                Total = sum.Total + c.Total,
                Passed = sum.Passed + c.Passed,
                Failed = sum.Failed + c.Failed,
                Errored = sum.Errored + c.Errored,
                Cancelled = sum.Cancelled + c.Cancelled,
                TimedOut = sum.TimedOut + c.TimedOut,
                Skipped = sum.Skipped + c.Skipped
            };
        }

        return sum;
    }

    /// <summary>
    /// Builds the structured record of what was merged. This is what the report renders and what a
    /// pipeline reads; nothing about the merge is left to be inferred from a sentence.
    /// </summary>
    private static MergeInfo BuildMergeInfo(
        IReadOnlyList<TraceMergeInput> inputs, double summedMs, double wallClockMs)
    {
        var ordered = inputs
            .Select((input, index) => (input, index))
            .OrderBy(x => x.input.Trace.StartedAt)
            .ToArray();

        var sources = new List<MergeSource>(inputs.Count);
        DateTimeOffset? previousEnd = null;

        foreach (var (input, index) in ordered)
        {
            var trace = input.Trace;

            // Idle time since the previous run ended: the restart, the queue wait, the manual step.
            // Negative would mean the runs overlapped, which is not a gap.
            var gap = previousEnd is null
                ? (double?)null
                : Math.Max(0, (trace.StartedAt - previousEnd.Value).TotalMilliseconds);

            sources.Add(new MergeSource
            {
                Index = index,
                IdPrefix = InputSegment(index),
                Source = input.Source,
                ToolVersion = trace.ToolVersion,
                StartedAt = trace.StartedAt,
                EndedAt = trace.EndedAt,
                DurationMs = DurationOf(trace),
                Outcome = trace.Outcome,
                ExitCode = trace.ExitCode,
                SuiteCount = trace.Suites.Count,
                Counts = trace.Counts,
                GapBeforeMs = gap,
                Run = trace.Run,
                Environment = trace.Environment
            });

            previousEnd = previousEnd is null || trace.EndedAt > previousEnd ? trace.EndedAt : previousEnd;
        }

        return new MergeInfo
        {
            TestDurationMs = summedMs,
            ElapsedMs = wallClockMs,
            BetweenRunsMs = Math.Max(0, wallClockMs - summedMs),
            Sources = sources
        };
    }

    /// <summary>
    /// The inputs' own root diagnostics, plus anything genuinely worth warning about. The merge's
    /// own facts are NOT written here — they belong in <see cref="MergeInfo"/>, where the report can
    /// lay them out and a pipeline can read them.
    /// </summary>
    private static IReadOnlyList<Diagnostic>? BuildDiagnostics(IReadOnlyList<TraceMergeInput> inputs)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var input in inputs)
        {
            foreach (var diagnostic in input.Trace.Diagnostics ?? [])
                diagnostics.Add(diagnostic);
        }

        var versions = DistinctToolVersions(inputs);
        if (versions.Length > 1)
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Message = $"Merged runs were produced by different JTest versions ({string.Join(", ", versions)})."
            });
        }

        return diagnostics.Count == 0 ? null : diagnostics;
    }

}
