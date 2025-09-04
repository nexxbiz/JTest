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
    private bool _headerWritten = false;

    public void LogStepExecution(StepDebugInfo stepInfo)
    {
        // Write header only once per debug session
        if (!_headerWritten)
        {
            WriteDebugReportHeader(stepInfo.TestFileName);
            _headerWritten = true;
        }

        WriteTestStepHeader(stepInfo);
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
        // Removed: WriteAssertionGuidance(changes); - removes clutter from output
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
        _output.AppendLine();
        _output.AppendLine($"## Test {stepInfo.TestNumber}, Step {stepInfo.StepNumber}: {stepInfo.StepType}");
        _output.AppendLine();
    }

    private void WriteDebugReportHeader(string testFileName)
    {
        if (string.IsNullOrEmpty(testFileName))
            testFileName = "test-file.json";

        _output.AppendLine($"# Debug Report for {testFileName}");
        _output.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.AppendLine($"**Test File:** {testFileName}");
        _output.AppendLine();
        _output.AppendLine("---");
        _output.AppendLine();
    }

    private void WriteTestStepHeader(StepDebugInfo stepInfo)
    {
        _output.AppendLine();
        
        // Enhanced test identification
        if (!string.IsNullOrEmpty(stepInfo.TestName))
        {
            _output.AppendLine($"## Test {stepInfo.TestNumber} - {stepInfo.TestName}");
            if (!string.IsNullOrEmpty(stepInfo.TestDescription))
            {
                _output.AppendLine(stepInfo.TestDescription);
            }
            _output.AppendLine();
            
            // Show step info with template name for UseStep
            if (stepInfo.StepType.Equals("UseStep", StringComparison.OrdinalIgnoreCase) && stepInfo.TemplateExecution != null)
            {
                _output.AppendLine($"**Step:** use {stepInfo.TemplateExecution.TemplateName}");
            }
            else if (!string.IsNullOrEmpty(stepInfo.StepId))
            {
                _output.AppendLine($"**Step:** {stepInfo.StepId}");
            }
            else
            {
                _output.AppendLine($"**Step:** {stepInfo.StepType}");
            }
        }
        else
        {
            // Fallback to original format if test name not available
            _output.AppendLine($"## Test {stepInfo.TestNumber}, Step {stepInfo.StepNumber}: {stepInfo.StepType}");
        }
        _output.AppendLine();
    }

    private void WriteStepDetails(StepDebugInfo stepInfo)
    {
        WriteStepIdentification(stepInfo);
        WriteStepResult(stepInfo);
    }

    private void WriteStepIdentification(StepDebugInfo stepInfo)
    {
        if (!string.IsNullOrEmpty(stepInfo.StepId))
        {
            _output.AppendLine($"**Step ID:** {stepInfo.StepId}");
        }
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

        _output.AppendLine("**Assertions:**");
        _output.AppendLine();

        foreach (var result in assertionResults)
        {
            WriteImprovedAssertionResult(result);
        }
        _output.AppendLine();
    }

    private void WriteImprovedAssertionResult(AssertionResult result)
    {
        var status = result.Success ? "PASSED" : "FAILED";
        var statusIcon = result.Success ? "✅" : "❌";

        // Enhanced description with actual vs expected format
        var description = result.Description ?? $"{result.Operation} assertion";
        
        if (result.Success)
        {
            _output.AppendLine($"- {description} : {status} {statusIcon}");
        }
        else
        {
            var actualDisplay = FormatAssertionValue(result.ActualValue ?? "null");
            var expectedDisplay = FormatAssertionValue(result.ExpectedValue ?? "null");
            
            if (result.ExpectedValue != null)
            {
                _output.AppendLine($"- {description} : got `{actualDisplay}` : expected `{expectedDisplay}` : {status} {statusIcon}");
            }
            else
            {
                _output.AppendLine($"- {description} : got `{actualDisplay}` : {status} {statusIcon}");
            }
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _output.AppendLine($"  - Error: {result.ErrorMessage}");
            }
        }
        
        _output.AppendLine();
    }

    private void WriteAssertionResult(AssertionResult result)
    {
        var status = result.Success ? "PASSED" : "FAILED";
        var statusIcon = result.Success ? "✅" : "❌";

        _output.AppendLine($"**{result.Operation.ToUpperInvariant()}** - {status} {statusIcon}");

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

        // Input parameters with security masking
        if (templateInfo.InputParameters.Any())
        {
            WriteTemplateInputParameters(templateInfo.InputParameters);
        }

        // Template outputs
        if (templateInfo.OutputValues.Any())
        {
            WriteTemplateOutputs(templateInfo.OutputValues);
        }

        // Saved variables with detailed content
        if (templateInfo.SavedVariables.Any())
        {
            WriteTemplateSavedVariables(templateInfo.SavedVariables);
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

    private void WriteTemplateInputParameters(Dictionary<string, object> inputParameters)
    {
        _output.AppendLine("**Input Parameters:**");
        foreach (var param in inputParameters)
        {
            var maskedValue = MaskSecurityValue(param.Key, param.Value);
            _output.AppendLine($"- `{param.Key}`: {maskedValue}");
        }
        _output.AppendLine();
    }

    private void WriteTemplateOutputs(Dictionary<string, object> outputValues)
    {
        _output.AppendLine("**Template Outputs:**");
        foreach (var output in outputValues)
        {
            var valueDesc = DescribeValue(output.Value);
            _output.AppendLine($"- `{output.Key}`: {valueDesc}");
            
            // Add detailed section for complex objects
            if (IsComplexObject(output.Value))
            {
                WriteObjectDetails(output.Key, output.Value, "Template Output");
            }
        }
        _output.AppendLine();
    }

    private void WriteTemplateSavedVariables(Dictionary<string, object> savedVariables)
    {
        _output.AppendLine("**Saved variables:**");
        foreach (var saved in savedVariables)
        {
            WriteSavedVariableWithDetails(saved.Key, saved.Value);
            
            // Add detailed section for complex objects  
            if (IsComplexObject(saved.Value))
            {
                WriteObjectDetails(saved.Key, saved.Value, "Saved Variable");
            }
        }
        _output.AppendLine();
    }

    private string MaskSecurityValue(string key, object value)
    {
        // List of parameter names that should be masked for security
        var securityKeys = new[] { "password", "token", "secret", "key", "credential", "auth", "authorization" };
        
        if (securityKeys.Any(sk => key.ToLowerInvariant().Contains(sk)))
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                return "\"***masked***\"";
            }
            return "***masked***";
        }
        
        return DescribeValue(value);
    }

    private bool IsComplexObject(object value)
    {
        return value is Dictionary<string, object> || 
               value is Array || 
               value is System.Collections.IList ||
               (value != null && !IsSimpleType(value.GetType()));
    }

    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(decimal) || 
               type == typeof(DateTime) || 
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid);
    }

    private void WriteObjectDetails(string key, object value, string section)
    {
        _output.AppendLine($"  <details>");
        _output.AppendLine($"  <summary>View {key} details ({section})</summary>");
        _output.AppendLine();
        _output.AppendLine("  ```json");
        _output.AppendLine($"  {FormatObjectAsJson(value)}");
        _output.AppendLine("  ```");
        _output.AppendLine("  </details>");
    }

    private string DescribeValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        
        // Handle numeric types properly without quotes
        if (value is int || value is double || value is float || value is decimal || value is long)
            return value.ToString() ?? "unknown";

        if (value is IDictionary<string, object> dict)
            return $"{{object with {dict.Count} properties}}";

        return value.ToString() ?? "unknown";
    }

    /// <summary>
    /// Writes a saved variable with detailed content in a collapsible section for complex objects
    /// </summary>
    private void WriteSavedVariableWithDetails(string key, object value)
    {
        if (value == null)
        {
            _output.AppendLine($"- `{key}`: null");
            return;
        }

        // For simple values, show them inline
        if (value is string str)
        {
            _output.AppendLine($"- `{key}`: \"{str}\"");
            return;
        }

        if (value is int || value is double || value is float || value is decimal || value is long || value is bool)
        {
            _output.AppendLine($"- `{key}`: {value}");
            return;
        }

        // For complex objects (dictionaries, arrays), show summary and detailed content
        if (value is Dictionary<string, object> dict)
        {
            _output.AppendLine($"- `{key}`: {{object with {dict.Count} properties}}");
            _output.AppendLine($"  <details>");
            _output.AppendLine($"  <summary>View {key} details</summary>");
            _output.AppendLine();
            _output.AppendLine("  ```json");
            _output.AppendLine($"  {FormatObjectAsJson(value)}");
            _output.AppendLine("  ```");
            _output.AppendLine("  </details>");
            return;
        }

        if (value is Array array)
        {
            _output.AppendLine($"- `{key}`: [array with {array.Length} items]");
            _output.AppendLine($"  <details>");
            _output.AppendLine($"  <summary>View {key} details</summary>");
            _output.AppendLine();
            _output.AppendLine("  ```json");
            _output.AppendLine($"  {FormatObjectAsJson(value)}");
            _output.AppendLine("  ```");
            _output.AppendLine("  </details>");
            return;
        }

        if (value is System.Collections.IList list)
        {
            _output.AppendLine($"- `{key}`: [array with {list.Count} items]");
            _output.AppendLine($"  <details>");
            _output.AppendLine($"  <summary>View {key} details</summary>");
            _output.AppendLine();
            _output.AppendLine("  ```json");
            _output.AppendLine($"  {FormatObjectAsJson(value)}");
            _output.AppendLine("  ```");
            _output.AppendLine("  </details>");
            return;
        }

        // For other complex objects, try to serialize
        try
        {
            var typeName = value.GetType().Name;
            _output.AppendLine($"- `{key}`: {{object of type {typeName}}}");
            _output.AppendLine($"  <details>");
            _output.AppendLine($"  <summary>View {key} details</summary>");
            _output.AppendLine();
            _output.AppendLine("  ```json");
            _output.AppendLine($"  {FormatObjectAsJson(value)}");
            _output.AppendLine("  ```");
            _output.AppendLine("  </details>");
        }
        catch
        {
            // Fallback to simple description if serialization fails
            _output.AppendLine($"- `{key}`: {{object of type {value.GetType().Name}}}");
        }
    }

    /// <summary>
    /// Formats an object as JSON for detailed display
    /// </summary>
    private string FormatObjectAsJson(object value)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(value, options);
        }
        catch
        {
            return value.ToString() ?? "Unable to serialize";
        }
    }
}