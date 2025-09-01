using JTest.Core.Assertions;

namespace JTest.Core.Steps;

/// <summary>
/// Represents the result of step execution
/// </summary>
public class StepResult
{
    /// <summary>
    /// Gets or sets whether the step execution was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the step execution data to be stored in context
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Gets or sets the execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// Gets or sets the assertion results from step execution
    /// </summary>
    public List<AssertionResult> AssertionResults { get; set; } = new();
    
    /// <summary>
    /// Creates a successful step result
    /// </summary>
    public static StepResult CreateSuccess(object? data = null, long durationMs = 0)
    {
        return new StepResult { Success = true, Data = data, DurationMs = durationMs };
    }
    
    /// <summary>
    /// Creates a failed step result
    /// </summary>
    public static StepResult CreateFailure(string errorMessage, long durationMs = 0)
    {
        return new StepResult { Success = false, ErrorMessage = errorMessage, DurationMs = durationMs };
    }
}