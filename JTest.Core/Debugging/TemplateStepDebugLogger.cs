using JTest.Core.Assertions;

namespace JTest.Core.Debugging;

/// <summary>
/// Debug logger that captures template step execution details without outputting separate step headers
/// </summary>
public class TemplateStepDebugLogger : IDebugLogger
{
    private readonly List<TemplateStepExecutionInfo> _capturedSteps = new();
    
    public void LogStepExecution(StepDebugInfo stepInfo)
    {
        // Capture the step execution info but don't output a separate step header
        var executionInfo = new TemplateStepExecutionInfo
        {
            StepType = stepInfo.StepType,
            StepId = stepInfo.StepId,
            Result = stepInfo.Result,
            Duration = stepInfo.Duration,
            Enabled = stepInfo.Enabled,
            Description = stepInfo.Description
        };
        
        _capturedSteps.Add(executionInfo);
    }
    
    public void LogContextChanges(ContextChanges changes)
    {
        // Capture context changes for the last step if any
        if (_capturedSteps.Any())
        {
            var lastStep = _capturedSteps.Last();
            lastStep.ContextChanges = changes;
        }
    }
    
    public void LogRuntimeContext(Dictionary<string, object> context)
    {
        // Capture runtime context for the last step if any
        if (_capturedSteps.Any())
        {
            var lastStep = _capturedSteps.Last();
            lastStep.RuntimeContext = new Dictionary<string, object>(context);
        }
    }
    
    public void LogAssertionResults(List<AssertionResult> assertionResults)
    {
        // Capture assertion results for the last step if any
        if (_capturedSteps.Any())
        {
            var lastStep = _capturedSteps.Last();
            lastStep.AssertionResults = new List<AssertionResult>(assertionResults);
        }
    }
    
    /// <summary>
    /// Gets all captured step execution information
    /// </summary>
    public List<TemplateStepExecutionInfo> GetCapturedSteps() => new(_capturedSteps);
}

/// <summary>
/// Information about a step executed within a template
/// </summary>
public class TemplateStepExecutionInfo
{
    public string StepType { get; set; } = "";
    public string StepId { get; set; } = "";
    public string Result { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public bool Enabled { get; set; }
    public string Description { get; set; } = "";
    public ContextChanges? ContextChanges { get; set; }
    public Dictionary<string, object>? RuntimeContext { get; set; }
    public List<AssertionResult>? AssertionResults { get; set; }
}