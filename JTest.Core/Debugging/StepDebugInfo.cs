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
    public bool Enabled { get; set; }
    public string Result { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Description { get; set; } = string.Empty;
}