using JTest.Core.Tracing;

namespace JTest.Core.Reporting.Html;

/// <summary>
/// Orders suites, cases, and datasets failure-first (FR-019) so failures surface ahead of passing
/// detail even before JavaScript runs. Steps and loop iterations keep their natural execution
/// order (ordinal/index) — only the coarse navigation levels are reordered. Ordering is stable.
/// </summary>
internal static class FailureFirst
{
    private static int Rank(Outcome outcome) => outcome switch
    {
        Outcome.Errored => 0,
        Outcome.Failed => 1,
        Outcome.TimedOut => 2,
        Outcome.Cancelled => 3,
        Outcome.Passed => 4,
        Outcome.Skipped => 5,
        _ => 9
    };

    public static ExecutionTrace Order(ExecutionTrace trace) =>
        trace with { Suites = trace.Suites.OrderBy(s => Rank(s.Outcome)).Select(Suite).ToList() };

    private static SuiteResult Suite(SuiteResult s) =>
        s with { Cases = s.Cases.OrderBy(c => Rank(c.Outcome)).Select(Case).ToList() };

    private static CaseResult Case(CaseResult c) =>
        c with { Datasets = c.Datasets.OrderBy(d => Rank(d.Outcome)).ToList() };
}
