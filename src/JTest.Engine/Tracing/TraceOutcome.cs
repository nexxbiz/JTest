namespace JTest.Engine.Tracing;

/// <summary>
/// The outcome of one trace node. Aggregation precedence (strongest wins):
/// error, timedOut, cancelled, failed, skipped, passed.
/// </summary>
public enum TraceOutcome
{
    /// <summary>Everything the node claims to cover succeeded.</summary>
    Passed,

    /// <summary>The node was not executed because an earlier node failed or the run stopped.</summary>
    Skipped,

    /// <summary>An assertion or step-level check did not hold.</summary>
    Failed,

    /// <summary>The run was cancelled while this node was active or pending.</summary>
    Cancelled,

    /// <summary>A declared timeout elapsed.</summary>
    TimedOut,

    /// <summary>Execution itself failed unexpectedly.</summary>
    Error,
}
