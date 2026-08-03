namespace JTest.Core.Steps;

/// <summary>
/// Represents the result of a single step execution
/// </summary>
public sealed class StepExecutionResult(
    Dictionary<string, object?>? data,
    IEnumerable<StepProcessedResult>? innerProcessedResults = null,
    IEnumerable<StepIteration>? iterations = null,
    bool timedOut = false,
    bool cancelled = false)
{
    public Dictionary<string, object?>? Data { get; } = data;

    /// <summary>All inner step results (flattened across iterations for loops).</summary>
    public IEnumerable<StepProcessedResult> InnerProcessedResults { get; } = innerProcessedResults ?? [];

    /// <summary>Per-iteration results for loop steps; empty for non-loop steps (FR-013).</summary>
    public IEnumerable<StepIteration> Iterations { get; } = iterations ?? [];

    /// <summary>The step exceeded a configured timeout (FR-007) — a distinct outcome, not a pass.</summary>
    public bool TimedOut { get; } = timedOut;

    /// <summary>The step was cancelled (FR-006) — a distinct outcome, not a pass.</summary>
    public bool Cancelled { get; } = cancelled;
}
