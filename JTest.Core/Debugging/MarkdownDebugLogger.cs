using System.Globalization;
using System.Text;
using System.Text.Json;
using JTest.Core.Assertions;

namespace JTest.Core.Debugging;

/// <summary>
/// Markdown debug logger implementation that generates formatted debug output
/// </summary>
public class MarkdownDebugLogger : IDebugLogger
{
    private readonly StringBuilder _output = new();
    
    public void LogStepExecution(StepDebugInfo stepInfo)
    {
        WriteStepHeader(stepInfo);
        WriteStepDetails(stepInfo);
        
        // Add template execution details for UseStep in a collapsible section
        if (stepInfo.TemplateExecution != null)
        {
            WriteTemplateExecutionDetailsCollapsible(stepInfo.TemplateExecution);
        }
    }
    
    public void LogContextChanges(ContextChanges changes)
    {
        WriteContextChanges(changes);
        WriteAssertionGuidance(changes);
    }
    
    public void LogRuntimeContext(Dictionary<string, object> context)
    {
        WriteRuntimeContext(context);
    }

    public void LogAssertionResults(List<AssertionResult> assertionResults)
    {
        WriteAssertionResults(assertionResults);
    }

    public string GetOutput() => _output.ToString();

    private void WriteStepHeader(StepDebugInfo stepInfo)
    {
        _output.AppendLine($"## Test {stepInfo.TestNumber}, Step {stepInfo.StepNumber}: {stepInfo.StepType}");
    }

    private void WriteStepDetails(StepDebugInfo stepInfo)
    {
        WriteStepIdentification(stepInfo);
        WriteStepResult(stepInfo);
    }

    private void WriteStepIdentification(StepDebugInfo stepInfo)
    {
        if (!string.IsNullOrEmpty(stepInfo.StepId))
            _output.AppendLine($"**Step ID:** {stepInfo.StepId}");
        _output.AppendLine($"**Step Type:** {stepInfo.StepType}");
        _output.AppendLine($"**Enabled:** {stepInfo.Enabled}");
        _output.AppendLine();
    }

    private void WriteStepResult(StepDebugInfo stepInfo)
    {
        _output.AppendLine($"**Result:** {stepInfo.Result}");
        _output.AppendLine($"**Duration:** {FormatDuration(stepInfo.Duration)}");
        _output.AppendLine();
    }

    private void WriteContextChanges(ContextChanges changes)
    {
        if (HasContextChanges(changes))
        {
            _output.AppendLine("**Context Changes:**");
            WriteAddedVariables(changes.Added);
            WriteModifiedVariables(changes.Modified);
        }
        else
        {
            _output.AppendLine("**Context Changes:** None");
        }
        _output.AppendLine();
    }

    private void WriteAssertionGuidance(ContextChanges changes)
    {
        if (changes.Available.Any())
        {
            _output.AppendLine("**For Assertions:** You can now reference these JSONPath expressions:");
            WriteAvailableExpressions(changes.Available);
            _output.AppendLine();
        }
    }

    private void WriteRuntimeContext(Dictionary<string, object> context)
    {
        WriteDetailsHeader();
        WriteJsonContent(context);
        WriteDetailsFooter();
    }

    private void WriteAssertionResults(List<AssertionResult> assertionResults)
    {
        if (!assertionResults.Any()) return;
        
        _output.AppendLine("**Assertion Results:**");
        _output.AppendLine();
        
        foreach (var result in assertionResults)
        {
            WriteAssertionResult(result);
        }
        _output.AppendLine();
    }

    private void WriteAssertionResult(AssertionResult result)
    {
        var status = result.Success ? "PASSED" : "FAILED";
        
        _output.AppendLine($"**{result.Operation.ToUpperInvariant()}** - {status}");
        
        if (!string.IsNullOrEmpty(result.Description))
            _output.AppendLine($"  - Description: {result.Description}");
            
        if (result.ActualValue != null)
            _output.AppendLine($"  - Actual: `{FormatAssertionValue(result.ActualValue)}`");
            
        if (result.ExpectedValue != null)
            _output.AppendLine($"  - Expected: `{FormatAssertionValue(result.ExpectedValue)}`");
            
        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            _output.AppendLine($"  - Error: {result.ErrorMessage}");
            
        _output.AppendLine();
    }

    private string FormatAssertionValue(object value)
    {
        return value switch
        {
            null => "null",
            string str => str.Length > 50 ? $"{str[..47]}..." : str,
            JsonElement jsonElement => jsonElement.ToString(),
            _ => value.ToString() ?? "null"
        };
    }

    private void WriteDetailsHeader()
    {
        _output.AppendLine("<details>");
        _output.AppendLine("<summary>Runtime Context (Click to expand)</summary>");
        _output.AppendLine();
    }

    private void WriteJsonContent(Dictionary<string, object> context)
    {
        _output.AppendLine("```json");
        _output.AppendLine(FormatContextAsJson(context));
        _output.AppendLine("```");
        _output.AppendLine();
    }

    private void WriteDetailsFooter()
    {
        _output.AppendLine("</details>");
    }

    private string FormatDuration(TimeSpan duration)
    {
        var milliseconds = duration.TotalMilliseconds;
        return milliseconds.ToString("F2", new CultureInfo("de-DE")) + "ms";
    }

    private bool HasContextChanges(ContextChanges changes) =>
        changes.Added.Any() || changes.Modified.Any();

    private void WriteAddedVariables(List<string> added)
    {
        if (!added.Any()) return;
        _output.AppendLine();
        _output.AppendLine("**Added:**");
        foreach (var variable in added)
            _output.AppendLine($"- {variable}");
    }

    private void WriteModifiedVariables(List<string> modified)
    {
        if (!modified.Any()) return;
        _output.AppendLine();
        _output.AppendLine("**Modified:**");
        foreach (var variable in modified)
            _output.AppendLine($"- {variable}");
    }

    private void WriteAvailableExpressions(List<string> available)
    {
        foreach (var expr in available)
        {
            _output.AppendLine($"- `{expr}` or `{{{{ {expr} }}}}`");
            // Only show examples for complex object paths, not simple values
            if (expr.Contains("execute-workflow") || expr.Contains("workflow") && !expr.EndsWith("Id"))
                WriteExampleUsage(expr);
        }
    }

    private void WriteExampleUsage(string expression)
    {
        _output.AppendLine($"  - Example: `{expression}.status`");
    }

    private string FormatContextAsJson(Dictionary<string, object> context)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        return JsonSerializer.Serialize(context, options);
    }

    private void WriteTemplateExecutionDetails(TemplateExecutionInfo templateInfo)
    {
        _output.AppendLine("### Template Execution Details");
        _output.AppendLine();
        
        _output.AppendLine($"**Template:** {templateInfo.TemplateName}");
        _output.AppendLine($"**Steps Executed:** {templateInfo.StepsExecuted}");
        _output.AppendLine();
        
        // Input parameters
        if (templateInfo.InputParameters.Any())
        {
            _output.AppendLine("**Input Parameters:**");
            foreach (var param in templateInfo.InputParameters)
            {
                var valueDesc = DescribeValue(param.Value);
                _output.AppendLine($"- `{param.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
        
        // Template outputs
        if (templateInfo.OutputValues.Any())
        {
            _output.AppendLine("**Template Outputs:**");
            foreach (var output in templateInfo.OutputValues)
            {
                var valueDesc = DescribeValue(output.Value);
                _output.AppendLine($"- `{output.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
        
        // Saved variables
        if (templateInfo.SavedVariables.Any())
        {
            _output.AppendLine("**Variables Saved:**");
            foreach (var saved in templateInfo.SavedVariables)
            {
                var valueDesc = DescribeValue(saved.Value);
                _output.AppendLine($"- `{saved.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
    }

    private void WriteTemplateExecutionDetailsCollapsible(TemplateExecutionInfo templateInfo)
    {
        _output.AppendLine("<details>");
        _output.AppendLine("<summary>Template Execution Details (Click to expand)</summary>");
        _output.AppendLine();
        
        _output.AppendLine($"**Template:** {templateInfo.TemplateName}");
        _output.AppendLine($"**Steps Executed:** {templateInfo.StepsExecuted}");
        _output.AppendLine();
        
        // Input parameters
        if (templateInfo.InputParameters.Any())
        {
            _output.AppendLine("**Input Parameters:**");
            foreach (var param in templateInfo.InputParameters)
            {
                var valueDesc = DescribeValue(param.Value);
                _output.AppendLine($"- `{param.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
        
        // Template outputs
        if (templateInfo.OutputValues.Any())
        {
            _output.AppendLine("**Template Outputs:**");
            foreach (var output in templateInfo.OutputValues)
            {
                var valueDesc = DescribeValue(output.Value);
                _output.AppendLine($"- `{output.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
        
        // Saved variables
        if (templateInfo.SavedVariables.Any())
        {
            _output.AppendLine("**Variables Saved:**");
            foreach (var saved in templateInfo.SavedVariables)
            {
                var valueDesc = DescribeValue(saved.Value);
                _output.AppendLine($"- `{saved.Key}`: {valueDesc}");
            }
            _output.AppendLine();
        }
        
        // Step execution details
        if (templateInfo.StepExecutionDetails?.Any() == true)
        {
            _output.AppendLine("**Step Execution Details:**");
            foreach (var stepDetail in templateInfo.StepExecutionDetails)
            {
                _output.AppendLine($"- **{stepDetail.StepType}** ({stepDetail.StepId}): {stepDetail.Result} in {FormatDuration(stepDetail.Duration)}");
            }
            _output.AppendLine();
        }
        
        _output.AppendLine("</details>");
        _output.AppendLine();
    }

    private string DescribeValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        
        if (value is IDictionary<string, object> dict)
            return $"{{object with {dict.Count} properties}}";
        
        return value.ToString() ?? "unknown";
    }
}