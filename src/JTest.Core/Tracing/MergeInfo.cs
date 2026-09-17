namespace JTest.Core.Tracing;

/// <summary>
/// Present only on a trace produced by merging several runs. It records what the merged root's
/// numbers mean and where they came from, so a reader is never left inferring it from prose — and a
/// pipeline can read it without parsing English.
/// </summary>
public sealed record MergeInfo
{
    /// <summary>How <see cref="ExecutionTrace.DurationMs"/> was computed. Currently always
    /// <c>sumOfSources</c>: the sum of the merged runs' own durations.</summary>
    public string DurationSemantics { get; init; } = "sumOfSources";

    /// <summary>Time actually spent running tests: the sum of the sources' durations.</summary>
    public required double TestDurationMs { get; init; }

    /// <summary>Wall-clock from the earliest start to the latest end, gaps included.</summary>
    public required double ElapsedMs { get; init; }

    /// <summary>Wall clock minus test time — the restarts, queue waits and manual steps between runs.</summary>
    public required double BetweenRunsMs { get; init; }

    public required IReadOnlyList<MergeSource> Sources { get; init; }
}

/// <summary>One run that went into a merged trace.</summary>
public sealed record MergeSource
{
    /// <summary>Zero-based position in the merge, matching the <c>input[n]</c> id prefix.</summary>
    public required int Index { get; init; }

    /// <summary>The prefix every node from this run carries, e.g. <c>input[0]</c>.</summary>
    public required string IdPrefix { get; init; }

    /// <summary>The trace file this run was read from.</summary>
    public required string Source { get; init; }

    public required string ToolVersion { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset EndedAt { get; init; }
    public required double DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public required int ExitCode { get; init; }
    public required int SuiteCount { get; init; }
    public required Rollup Counts { get; init; }

    /// <summary>Idle time between the previous run's end and this one's start; null for the first.</summary>
    public double? GapBeforeMs { get; init; }

    /// <summary>
    /// This run's <c>$.run</c> values, kept here rather than at the merged root: they are
    /// per-invocation, so no single set of them is the merged run's.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Run { get; init; }

    /// <summary>This run's masked environment dump, kept per source for the same reason as <see cref="Run"/>.</summary>
    public IReadOnlyDictionary<string, object?>? Environment { get; init; }
}
