namespace JTest.Core.Tracing;

/// <summary>
/// Helpers for constructing trace nodes with stable ids, hierarchical execution paths, and
/// aggregated counts/outcomes (FR-011/012/014). Executors compose paths as they descend so
/// numbering and ancestry are captured at execution time and never collide across nesting.
/// </summary>
public static class TraceBuilder
{
    /// <summary>Compose a child execution path, e.g. Path("suite[0]/case[1]", "step", 3) → ".../step[3]".</summary>
    public static string Path(string parentPath, string segmentKind, int index)
    {
        var segment = $"{segmentKind}[{index}]";
        return string.IsNullOrEmpty(parentPath) ? segment : $"{parentPath}/{segment}";
    }

    /// <summary>A stable id for a node is its execution path (unique by construction).</summary>
    public static string Id(string path) => path;

    /// <summary>Aggregate a parent outcome from child outcomes (precedence in <see cref="OutcomeExtensions"/>).</summary>
    public static Outcome AggregateOutcome(IEnumerable<Outcome> children) =>
        OutcomeExtensions.Aggregate(children);

    /// <summary>Tally child outcomes into a <see cref="Rollup"/>.</summary>
    public static Rollup Counts(IEnumerable<Outcome> children) => Rollup.From(children);
}
