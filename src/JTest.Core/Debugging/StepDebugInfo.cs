namespace JTest.Core.Debugging;

/// <summary>
/// Debug information for a test step execution
/// </summary>
public class StepDebugInfo
{
    public int TestNumber { get; set; }
    public int StepNumber { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Test file name for header generation
    /// </summary>
    public string TestFileName { get; set; } = string.Empty;

    /// <summary>
    /// Test name for better identification
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Test description for context
    /// </summary>
    public string TestDescription { get; set; } = string.Empty;

    /// <summary>
    /// Test suite name for better identification
    /// </summary>
    public string TestSuiteName { get; set; } = string.Empty;

    /// <summary>
    /// Test suite description for context
    /// </summary>
    public string TestSuiteDescription { get; set; } = string.Empty;
}

