namespace JTest.Core.Steps;

/// <summary>
/// Represents the result of a single step execution
/// </summary>
public sealed class StepExecutionResult(Dictionary<string, object?>? data, IEnumerable<StepProcessedResult>? innerProcessedResults = null)
{
    public Dictionary<string, object?>? Data { get; } = data;
    public IEnumerable<StepProcessedResult> InnerProcessedResults { get; } = innerProcessedResults ?? [];
}