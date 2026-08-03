namespace JTest.Core.Tracing;

/// <summary>
/// The outcome of any trace node. Order of the enum is not significant; aggregation
/// precedence is defined explicitly in <see cref="OutcomeExtensions"/>.
/// </summary>
public enum Outcome
{
    Passed,
    Failed,
    Errored,
    Cancelled,
    TimedOut,
    Skipped
}

public static class OutcomeExtensions
{
    // Parent outcome precedence (most severe first): errored > timedOut > cancelled > failed > passed.
    // 'skipped' is ignored unless every child is skipped.
    private static readonly Outcome[] Precedence =
    {
        Outcome.Errored,
        Outcome.TimedOut,
        Outcome.Cancelled,
        Outcome.Failed,
        Outcome.Passed
    };

    /// <summary>Aggregate a parent outcome from its children (data-model.md rule).</summary>
    public static Outcome Aggregate(IEnumerable<Outcome> children)
    {
        var any = false;
        var allSkipped = true;
        var seen = new HashSet<Outcome>();

        foreach (var child in children)
        {
            any = true;
            if (child != Outcome.Skipped)
            {
                allSkipped = false;
                seen.Add(child);
            }
        }

        if (!any) return Outcome.Passed;   // a node with no children is passing by itself
        if (allSkipped) return Outcome.Skipped;

        foreach (var candidate in Precedence)
        {
            if (seen.Contains(candidate)) return candidate;
        }

        return Outcome.Passed;
    }

    /// <summary>True when the outcome must contribute to a non-zero exit (not passed/skipped).</summary>
    public static bool IsFailure(this Outcome outcome) =>
        outcome is not (Outcome.Passed or Outcome.Skipped);
}
