using JTest.Core.Steps;

namespace JTest.Core.Models;

/// <summary>
/// Represents the result of executing a test case with an optional dataset
/// </summary>
public class JTestCaseResult
{
    /// <summary>
    /// Gets or sets the test case name
    /// </summary>
    public string TestCaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dataset reference (if executed with dataset)
    /// </summary>
    public JTestDataset? Dataset { get; set; }

    /// <summary>
    /// Gets or sets whether the test case execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the step results from the test execution
    /// </summary>
    public List<StepResult> StepResults { get; set; } = new();

    /// <summary>
    /// Gets or sets any error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}