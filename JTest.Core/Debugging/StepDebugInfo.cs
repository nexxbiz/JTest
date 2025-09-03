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
    
    /// <summary>
    /// Template execution details (populated for UseStep)
    /// </summary>
    public TemplateExecutionInfo? TemplateExecution { get; set; }
}

/// <summary>
/// Details about template execution for UseStep debugging
/// </summary>
public class TemplateExecutionInfo
{
    /// <summary>
    /// Name of the template that was executed
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Input parameters provided to the template
    /// </summary>
    public Dictionary<string, object> InputParameters { get; set; } = new();
    
    /// <summary>
    /// Number of steps executed within the template
    /// </summary>
    public int StepsExecuted { get; set; }
    
    /// <summary>
    /// Output values mapped from template to parent context
    /// </summary>
    public Dictionary<string, object> OutputValues { get; set; } = new();
    
    /// <summary>
    /// Variables saved via save operations
    /// </summary>
    public Dictionary<string, object> SavedVariables { get; set; } = new();
}