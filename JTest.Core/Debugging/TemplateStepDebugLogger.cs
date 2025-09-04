using System.Globalization;
using System.Text;
using JTest.Core.Assertions;
using JTest.Core.Models;

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
    
    public void LogTestSummary(List<JTestCaseResult> testResults)
    {
        // Template logger doesn't output summaries
    }
    
    /// <summary>
    /// Gets all captured step execution information
    /// </summary>
    public List<TemplateStepExecutionInfo> GetCapturedSteps() => new(_capturedSteps);

    /// <summary>
    /// Gets formatted output for all captured steps using the same layout as MarkdownDebugLogger
    /// This is the "dry implementation" reusing MarkdownDebugLogger formatting logic
    /// </summary>
    public string GetOutput()
    {
        if (!_capturedSteps.Any()) return "";

        var output = new StringBuilder();
        output.AppendLine("**Template Step Execution Details:**");
        output.AppendLine();

        foreach (var stepDetail in _capturedSteps)
        {
            output.AppendLine($"- **{stepDetail.StepType}** ({stepDetail.StepId}): {stepDetail.Result} in {FormatDuration(stepDetail.Duration)}");
            
            // Add assertion details for each step if they exist - reusing MarkdownDebugLogger formatting
            if (stepDetail.AssertionResults?.Any() == true)
            {
                var assertionContent = MarkdownDebugLogger.FormatAssertionResultsToMarkdown(stepDetail.AssertionResults, "  ");
                // Remove the extra newlines and format for nested display
                var lines = assertionContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        output.AppendLine($"  {line}");
                    }
                }
            }
        }

        return output.ToString();
    }

    private string FormatDuration(TimeSpan duration)
    {
        var milliseconds = duration.TotalMilliseconds;
        return milliseconds.ToString("F2", new CultureInfo("de-DE")) + "ms";
    }
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