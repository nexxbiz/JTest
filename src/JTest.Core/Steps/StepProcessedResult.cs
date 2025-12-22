using JTest.Core.Assertions;
using JTest.Core.Debugging;

namespace JTest.Core.Steps;

/// <summary>
/// Represents the result of a step that is completely processed by the engine
/// </summary>
/// <param name="stepNumber"></param>
public sealed class StepProcessedResult(int stepNumber)
{
    /// <summary>
    ///  Stores the step that produced this result
    /// </summary>
    public required IStep Step { get; set; }

    /// <summary>
    /// Gets or sets whether the step execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets detailed description 
    /// </summary>
    public string DetailedDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the step execution data to be stored in context
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the assertion results from step execution
    /// </summary>
    public IEnumerable<AssertionResult> AssertionResults { get; init; } = [];

    /// <summary>
    /// Gets or sets the context changes from step execution (saved values)
    /// </summary>
    public ContextChanges? ContextChanges { get; init; }

    /// <summary>
    /// Creates a successful step result
    /// </summary>
    public static StepProcessedResult CreateSuccess(int stepNumber, IStep step, IEnumerable<AssertionResult>? assertionResults, object? data = null, long durationMs = 0)
    {
        return new StepProcessedResult(stepNumber)
        {
            Step = step,
            Success = true,
            Data = data,
            DurationMs = durationMs,
            AssertionResults = assertionResults ?? []
        };
    }

    /// <summary>
    /// Creates a failed step result
    /// </summary>
    public static StepProcessedResult CreateFailure(int stepNumber, IStep step, IEnumerable<AssertionResult>? assertionResults, string errorMessage, long durationMs = 0)
    {
        return new StepProcessedResult(stepNumber)
        {
            Step = step,
            Success = false,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            AssertionResults = assertionResults ?? []
        };
    }

    public string DetailedAssertionFailures => string.Join('-', AssertionResults.Where(a => a.Success == false).Select(a => a.ErrorMessage));

    /// <summary>
    /// List of inner step results if this step contains nested steps (e.g., a template step)
    /// </summary>
    public IEnumerable<StepProcessedResult> InnerResults { get; init; } = [];

    public int StepNumber { get; } = stepNumber;
}
