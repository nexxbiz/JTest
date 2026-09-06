namespace JTest.Core.Tracing;

/// <summary>Aggregate outcome counts for a run/suite/case/dataset node.</summary>
public sealed record Rollup
{
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Errored { get; init; }
    public int Cancelled { get; init; }
    public int TimedOut { get; init; }
    public int Skipped { get; init; }

    public static readonly Rollup Empty = new();

    /// <summary>Tally a set of child outcomes into a rollup.</summary>
    public static Rollup From(IEnumerable<Outcome> outcomes)
    {
        int total = 0, passed = 0, failed = 0, errored = 0, cancelled = 0, timedOut = 0, skipped = 0;

        foreach (var outcome in outcomes)
        {
            total++;
            switch (outcome)
            {
                case Outcome.Passed: passed++; break;
                case Outcome.Failed: failed++; break;
                case Outcome.Errored: errored++; break;
                case Outcome.Cancelled: cancelled++; break;
                case Outcome.TimedOut: timedOut++; break;
                case Outcome.Skipped: skipped++; break;
            }
        }

        return new Rollup
        {
            Total = total,
            Passed = passed,
            Failed = failed,
            Errored = errored,
            Cancelled = cancelled,
            TimedOut = timedOut,
            Skipped = skipped
        };
    }
}
