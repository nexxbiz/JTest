namespace JTest.Core.Steps;

/// <summary>
/// Represents the result of a single step execution
/// </summary>
public sealed class StepExecutionResult(
    Dictionary<string, object?>? data,
    IEnumerable<StepProcessedResult>? innerProcessedResults = null,
    IEnumerable<StepIteration>? iterations = null)
{
    public Dictionary<string, object?>? Data { get; } = data;

    /// <summary>All inner step results (flattened across iterations for loops).</summary>
    public IEnumerable<StepProcessedResult> InnerProcessedResults { get; } = innerProcessedResults ?? [];

    /// <summary>Per-iteration results for loop steps; empty for non-loop steps (FR-013).</summary>
    public IEnumerable<StepIteration> Iterations { get; } = iterations ?? [];
}
