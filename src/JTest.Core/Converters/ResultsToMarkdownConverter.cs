using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Assertions;
using JTest.Core.Debugging;
using System.Text;
using System.Text.Json;

namespace JTest.Core.Converters;

public class ResultsToMarkdownConverter
{
    private readonly SecurityMasker _securityMasker = new();
    
    public string ConvertToMarkdown(List<JTestCaseResult> results)
    {
        var content = new StringBuilder();
        content.AppendLine();
        
        foreach (var result in results)
        {
            AppendTestCaseResult(content, result);
        }
        
        return _securityMasker.ApplyMasking(content.ToString());
    }

    private void AppendTestCaseResult(StringBuilder content, JTestCaseResult result)
    {
        content.AppendLine($"---");
        content.AppendLine();
        content.AppendLine($"### Test: {result.TestCaseName}");
        content.AppendLine();
        
        AppendTestCaseHeader(content, result);
        AppendStepResults(content, result.StepResults);
        content.AppendLine();
    }

    private void AppendTestCaseHeader(StringBuilder content, JTestCaseResult result)
    {
        var status = result.Success ? "PASSED" : "FAILED";
        content.AppendLine($"**Status:** {status}");
        content.AppendLine($"**Duration:** {result.DurationMs}ms");
        
        if (result.Dataset != null)
        {
            content.AppendLine($"**Dataset:** {result.Dataset.Name ?? "unnamed"}");
        }
        
        content.AppendLine();
    }

    private void AppendStepResults(StringBuilder content, List<StepResult> stepResults)
    {
        if (stepResults.Count == 0) return;
        
        content.AppendLine("#### Steps");
        content.AppendLine();
        
        foreach (var step in stepResults)
        {
            AppendStepResult(content, step);
        }
    }

    private void AppendStepResult(StringBuilder content, StepResult step)
    {
        var status = step.Success ? "PASSED" : "FAILED";
        content.AppendLine($"- **Step:** {status} ({step.DurationMs}ms)");
        
        if (!string.IsNullOrEmpty(step.DetailedDescription))
        {
            content.AppendLine($"{step.DetailedDescription}");
        }
        
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("error", step.ErrorMessage);
            content.AppendLine($"- **Error:** {step.ErrorMessage}");
        }
        
        AppendSavedValues(content, step.ContextChanges);
        AppendHttpRequestDetails(content, step);
        AppendAssertionResults(content, step.AssertionResults);
        AppendInnerSteps(content, step.InnerResults);
        
        content.AppendLine();
    }

    private void AppendHttpRequestDetails(StringBuilder content, StepResult step)
    {
        // Only show HTTP request details for HTTP steps
        if (step.Step.Type != "http" || step.Data == null) return;

        var requestData = ExtractRequestDetails(step.Data);
        if (requestData == null) return;

        content.AppendLine("**HTTP Request:**");
        content.AppendLine();
        
        // Use HTML table for HTTP request details
        content.AppendLine("<table>");
        content.AppendLine("<thead>");
        content.AppendLine("<tr><th>Field</th><th>Value</th></tr>");
        content.AppendLine("</thead>");
        content.AppendLine("<tbody>");

        // Add URL
        var url = System.Net.WebUtility.HtmlEncode(requestData.Url ?? "");
        content.AppendLine($"<tr><td>URL</td><td>{url}</td></tr>");

        // Add Method
        var method = System.Net.WebUtility.HtmlEncode(requestData.Method ?? "");
        content.AppendLine($"<tr><td>Method</td><td>{method}</td></tr>");

        // Add Headers
        if (requestData.Headers != null && requestData.Headers.Length > 0)
        {
            var headersDisplay = FormatHttpHeaders(requestData.Headers);
            content.AppendLine($"<tr><td>Headers</td><td>{headersDisplay}</td></tr>");
        }

        // Add Body
        if (!string.IsNullOrEmpty(requestData.Body))
        {
            var bodyDisplay = FormatHttpBody(requestData.Body);
            content.AppendLine($"<tr><td>Body</td><td>{bodyDisplay}</td></tr>");
        }

        content.AppendLine("</tbody>");
        content.AppendLine("</table>");
        content.AppendLine();
    }

    private HttpRequestDetails? ExtractRequestDetails(object stepData)
    {
        try
        {
            var dataType = stepData.GetType();
            var requestProperty = dataType.GetProperty("request");
            if (requestProperty == null) return null;

            var requestData = requestProperty.GetValue(stepData);
            if (requestData == null) return null;

            var requestType = requestData.GetType();
            var url = requestType.GetProperty("url")?.GetValue(requestData)?.ToString();
            var method = requestType.GetProperty("method")?.GetValue(requestData)?.ToString();
            var headers = requestType.GetProperty("headers")?.GetValue(requestData) as object[];
            var body = requestType.GetProperty("body")?.GetValue(requestData)?.ToString();

            return new HttpRequestDetails
            {
                Url = url,
                Method = method,
                Headers = headers,
                Body = body
            };
        }
        catch
        {
            // If we can't extract request details, just don't show them
            return null;
        }
    }

    private string FormatHttpHeaders(object[] headers)
    {
        try
        {
            var headerStrings = new List<string>();
            foreach (var header in headers)
            {
                var headerType = header.GetType();
                var name = headerType.GetProperty("name")?.GetValue(header)?.ToString();
                var value = headerType.GetProperty("value")?.GetValue(header)?.ToString();
                
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    // Register sensitive headers for masking
                    if (IsSensitiveHeader(name))
                    {
                        _securityMasker.RegisterForMasking(name, value);
                    }
                    
                    var encodedName = System.Net.WebUtility.HtmlEncode(name);
                    var encodedValue = System.Net.WebUtility.HtmlEncode(value);
                    headerStrings.Add($"{encodedName}: {encodedValue}");
                }
            }
            
            if (headerStrings.Count == 0) return "None";
            
            return $"<pre>{string.Join("<br/>", headerStrings)}</pre>";
        }
        catch
        {
            return "Unable to display headers";
        }
    }

    private string FormatHttpBody(string body)
    {
        if (string.IsNullOrEmpty(body)) return "Empty";

        // Register body for masking in case it contains sensitive data
        _securityMasker.RegisterForMasking("requestBody", body);

        // Try to format as JSON if it looks like JSON
        if (IsJsonString(body))
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
                var formatted = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                var encoded = System.Net.WebUtility.HtmlEncode(formatted);
                return $"<details><summary>show JSON</summary><pre>{encoded}</pre></details>";
            }
            catch
            {
                // Fall back to plain text display
            }
        }

        // Display as plain text
        var encodedBody = System.Net.WebUtility.HtmlEncode(body);
        return $"<pre>{encodedBody}</pre>";
    }

    private bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[] { "authorization", "x-api-key", "x-auth-token", "cookie", "set-cookie" };
        return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }

    private bool IsJsonString(string str)
    {
        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
    }

    private void AppendAssertionResults(StringBuilder content, List<AssertionResult> assertions)
    {
        if (assertions.Count == 0) return;
        
        content.AppendLine("**Assertions:**");
        content.AppendLine();
        
        // Use HTML table for assertions
        content.AppendLine("<table>");
        content.AppendLine("<thead>");
        content.AppendLine("<tr><th>Status</th><th>Description</th><th>Actual</th><th>Expected</th></tr>");
        content.AppendLine("</thead>");
        content.AppendLine("<tbody>");
        
        foreach (var assertion in assertions)
        {
            AppendAssertionAsTableRow(content, assertion);
        }
        
        content.AppendLine("</tbody>");
        content.AppendLine("</table>");
        content.AppendLine();
    }

    private void AppendAssertionAsTableRow(StringBuilder content, AssertionResult assertion)
    {
        var status = assertion.Success ? "PASSED" : "FAILED";
        var description = string.IsNullOrEmpty(assertion.Description) 
            ? assertion.Operation 
            : assertion.Description;
        
        // HTML encode all values to prevent HTML issues
        description = System.Net.WebUtility.HtmlEncode(description ?? "");
        var actualValue = System.Net.WebUtility.HtmlEncode(assertion.ActualValue?.ToString() ?? "");
        var expectedValue = System.Net.WebUtility.HtmlEncode(assertion.ExpectedValue?.ToString() ?? "");
        
        // Register for masking before HTML encoding
        if (assertion.ActualValue != null)
        {
            _securityMasker.RegisterForMasking("actual", assertion.ActualValue);
        }
        if (assertion.ExpectedValue != null)
        {
            _securityMasker.RegisterForMasking("expected", assertion.ExpectedValue);
        }
        
        content.AppendLine($"<tr><td>{status}</td><td>{description}</td><td>{actualValue}</td><td>{expectedValue}</td></tr>");
    }

    private void AppendSingleAssertion(StringBuilder content, AssertionResult assertion)
    {
        var status = assertion.Success ? "PASSED" : "FAILED";
        var description = string.IsNullOrEmpty(assertion.Description) 
            ? assertion.Operation 
            : assertion.Description;
            
        content.AppendLine($"- {status}: {description}");
        
            AppendAssertionDetails(content, assertion);
    }

    private void AppendAssertionDetails(StringBuilder content, AssertionResult assertion)
    {
        var details = new List<string>();
        
        if (assertion.ActualValue != null || !string.IsNullOrEmpty(assertion.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("actual", assertion.ActualValue ?? "null");
            details.Add($"**Actual:** {assertion.ActualValue}");
        }
        
        if (assertion.ExpectedValue != null || !string.IsNullOrEmpty(assertion.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("expected", assertion.ExpectedValue??"null");
            details.Add($"**Expected:** {assertion.ExpectedValue}");
        }
        
        //if (!string.IsNullOrEmpty(assertion.ErrorMessage))
        //{
        //    _securityMasker.RegisterForMasking("error", assertion.ErrorMessage);
        //    details.Add($"**Error:** {assertion.ErrorMessage}");
        //}
        
        if (details.Count > 0)
        {
            content.AppendLine($"{string.Join(", \n", details)}\n");
        }
    }

    private void AppendSavedValues(StringBuilder content, ContextChanges? contextChanges)
    {
        if (contextChanges == null) return;
        if (contextChanges.Added.Count == 0 && contextChanges.Modified.Count == 0) return;
        
        content.AppendLine("**Saved Values:**");
        content.AppendLine();
        
        // Use HTML table instead of markdown table to support proper formatting
        var hasVariables = false;
        var tableContent = new StringBuilder();
        tableContent.AppendLine("<table>");
        tableContent.AppendLine("<thead>");
        tableContent.AppendLine("<tr><th>Action</th><th>Variable</th><th>Value</th></tr>");
        tableContent.AppendLine("</thead>");
        tableContent.AppendLine("<tbody>");
        
        foreach (var variable in contextChanges.Added)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValueForHtmlTable(variable.Value, variable.Key);
            tableContent.AppendLine($"<tr><td>Added</td><td>{variable.Key}</td><td>{valueDisplay}</td></tr>");
            hasVariables = true;
        }
        
        foreach (var variable in contextChanges.Modified)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValueForHtmlTable(variable.Value, variable.Key);
            tableContent.AppendLine($"<tr><td>Modified</td><td>{variable.Key}</td><td>{valueDisplay}</td></tr>");
            hasVariables = true;
        }
        
        tableContent.AppendLine("</tbody>");
        tableContent.AppendLine("</table>");
        
        if (hasVariables)
        {
            content.Append(tableContent.ToString());
        }
        
        content.AppendLine();
    }



    private bool ShouldSkipVariable(string variableName)
    {
        // Skip internal variables that are not user-created
        return variableName.Equals("this", StringComparison.OrdinalIgnoreCase);
    }

    private void AppendInnerSteps(StringBuilder content, List<StepResult> innerResults)
    {
        if (innerResults.Count == 0) return;
        
        // Wrap entire Template Steps section in collapsible panel
        content.AppendLine("<details>");
        content.AppendLine("<summary><strong>Template Steps</strong></summary>");
        content.AppendLine();
        
        // Use HTML table for template steps
        content.AppendLine("<table>");
        content.AppendLine("<thead>");
        content.AppendLine("<tr><th>Step</th><th>Status</th><th>Duration</th><th>Details</th></tr>");
        content.AppendLine("</thead>");
        content.AppendLine("<tbody>");
        
        foreach (var innerStep in innerResults)
        {
            AppendInnerStepResultAsTableRow(content, innerStep);
        }
        
        content.AppendLine("</tbody>");
        content.AppendLine("</table>");
        
        // Add variable details after the main table, properly separated
        foreach (var innerStep in innerResults)
        {
            AppendInnerStepVariableDetails(content, innerStep);
        }
        
        content.AppendLine("</details>");
        content.AppendLine();
    }

    private void AppendInnerStepResultAsTableRow(StringBuilder content, StepResult step)
    {
        var status = step.Success ? "PASSED" : "FAILED";
        var description = GetInnerStepDescription(step);
        var details = GetInnerStepTableDetails(step);
        
        // HTML encode the description and details to prevent HTML issues
        description = System.Net.WebUtility.HtmlEncode(description);
        details = System.Net.WebUtility.HtmlEncode(details);
        
        content.AppendLine($"<tr><td>{description}</td><td>{status}</td><td>{step.DurationMs}ms</td><td>{details}</td></tr>");
    }

    private void AppendInnerStepResult(StringBuilder content, StepResult step)
    {
        var status = step.Success ? "PASSED" : "FAILED";
        var description = GetInnerStepDescription(step);
        content.AppendLine($"- **{description}:** {status} ({step.DurationMs}ms)");
        
        AppendInnerStepDetails(content, step);
    }

    private string GetInnerStepTableDetails(StepResult step)
    {
        var details = new List<string>();
        
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("error", step.ErrorMessage);
            details.Add($"Error: {step.ErrorMessage}");
        }
        
        // Add saved values summary 
        if (step.ContextChanges != null && 
            (step.ContextChanges.Added.Count > 0 || step.ContextChanges.Modified.Count > 0))
        {
            var savedSummary = GetSavedValuesSummary(step.ContextChanges);
            if (!string.IsNullOrEmpty(savedSummary))
            {
                details.Add($"Saved: {savedSummary}");
            }
        }
        
        // Add assertions summary
        if (step.AssertionResults.Count > 0)
        {
            var assertionSummary = GetAssertionsSummary(step.AssertionResults);
            details.Add($"Assertions: {assertionSummary}");
        }
        
        return string.Join("; ", details);
    }

    private void AppendInnerStepVariableDetails(StringBuilder content, StepResult step)
    {
        // Only add variable details if there are variables and they should be displayed
        if (step.ContextChanges == null || 
            (step.ContextChanges.Added.Count == 0 && step.ContextChanges.Modified.Count == 0))
        {
            return;
        }
        
        // Check if there are any non-skipped variables
        var hasVisibleVariables = step.ContextChanges.Added.Any(v => !ShouldSkipVariable(v.Key)) ||
                                 step.ContextChanges.Modified.Any(v => !ShouldSkipVariable(v.Key));
        
        if (!hasVisibleVariables) return;
        
        // Add a section header for this step's variables
        var stepDescription = GetInnerStepDescription(step);
        content.AppendLine();
        content.AppendLine($"**Variables for {stepDescription}:**");
        content.AppendLine();
        
        AppendTemplateStepVariables(content, step.ContextChanges);
    }

    private void AppendTemplateStepVariables(StringBuilder content, ContextChanges contextChanges)
    {
        var hasVariables = false;
        var tableContent = new StringBuilder();
        tableContent.AppendLine("<table>");
        tableContent.AppendLine("<thead>");
        tableContent.AppendLine("<tr><th>Action</th><th>Variable</th><th>Value</th></tr>");
        tableContent.AppendLine("</thead>");
        tableContent.AppendLine("<tbody>");
        
        foreach (var variable in contextChanges.Added)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValueForHtmlTable(variable.Value, variable.Key);
            tableContent.AppendLine($"<tr><td>Added</td><td>{variable.Key}</td><td>{valueDisplay}</td></tr>");
            hasVariables = true;
        }
        
        foreach (var variable in contextChanges.Modified)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValueForHtmlTable(variable.Value, variable.Key);
            tableContent.AppendLine($"<tr><td>Modified</td><td>{variable.Key}</td><td>{valueDisplay}</td></tr>");
            hasVariables = true;
        }
        
        tableContent.AppendLine("</tbody>");
        tableContent.AppendLine("</table>");
        
        if (hasVariables)
        {
            content.Append(tableContent.ToString());
        }
        content.AppendLine();
    }

    private string GetInnerStepDescription(StepResult stepResult)
    {
        // Use DetailedDescription if available, otherwise fall back to step description or type
        if (!string.IsNullOrEmpty(stepResult.DetailedDescription))
        {
            return stepResult.DetailedDescription;
        }
        
        return !string.IsNullOrEmpty(stepResult.Step.GetStepDescription()) 
            ? stepResult.Step.GetStepDescription()
            : $"{stepResult.Step.Type} step";
    }

    private void AppendInnerStepDetails(StringBuilder content, StepResult step)
    {
        var details = new List<string>();
        
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("error", step.ErrorMessage);
            details.Add($"**Error:** {step.ErrorMessage}");
        }
        
        // Add saved values summary with first few variable names
        if (step.ContextChanges != null && 
            (step.ContextChanges.Added.Count > 0 || step.ContextChanges.Modified.Count > 0))
        {
            var savedSummary = GetSavedValuesSummary(step.ContextChanges);
            if (!string.IsNullOrEmpty(savedSummary))
            {
                details.Add($"**Saved:** {savedSummary}");
            }
        }
        
        // Add assertions summary
        if (step.AssertionResults.Count > 0)
        {
            var assertionSummary = GetAssertionsSummary(step.AssertionResults);
            details.Add($"**Assertions:** {assertionSummary}");
        }
        
        if (details.Count > 0)
        {
            content.AppendLine($"({string.Join(", ", details)})");
        }
        
        // Add full variable details as collapsible sections if needed
        if (step.ContextChanges != null && 
            (step.ContextChanges.Added.Count > 0 || step.ContextChanges.Modified.Count > 0))
        {
            AppendCollapsibleSavedValues(content, step.ContextChanges);
        }
    }

    private void AppendCollapsibleSavedValues(StringBuilder content, ContextChanges contextChanges)
    {
        var hasVariables = false;
        var variableDetails = new StringBuilder();
        
        foreach (var variable in contextChanges.Added)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValue(variable.Value, variable.Key);
            variableDetails.AppendLine($"- **Added:** {variable.Key} = {valueDisplay}");
            hasVariables = true;
        }
        
        foreach (var variable in contextChanges.Modified)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            _securityMasker.RegisterForMasking(variable.Key, variable.Value);
            var valueDisplay = FormatVariableValue(variable.Value, variable.Key);
            variableDetails.AppendLine($"- **Modified:** {variable.Key} = {valueDisplay}");
            hasVariables = true;
        }
        
        if (hasVariables)
        {
            content.AppendLine($"\n{variableDetails}\n");
        }
    }

    private string GetSavedValuesSummary(ContextChanges contextChanges)
    {
        var parts = new List<string>();
        
        if (contextChanges.Added.Count > 0)
        {
            var addedVars = contextChanges.Added.Keys
                .Where(k => !ShouldSkipVariable(k))
                .Take(3)
                .ToList();
            
            if (addedVars.Count > 0)
            {
                var summary = string.Join(", ", addedVars);
                if (contextChanges.Added.Count > 3)
                {
                    summary += $" + {contextChanges.Added.Count - 3} more";
                }
                parts.Add($"Added {summary}");
            }
        }
        
        if (contextChanges.Modified.Count > 0)
        {
            var modifiedVars = contextChanges.Modified.Keys
                .Where(k => !ShouldSkipVariable(k))
                .Take(3)
                .ToList();
                
            if (modifiedVars.Count > 0)
            {
                var summary = string.Join(", ", modifiedVars);
                if (contextChanges.Modified.Count > 3)
                {
                    summary += $" + {contextChanges.Modified.Count - 3} more";
                }
                parts.Add($"Modified {summary}");
            }
        }
        
        return string.Join("; ", parts);
    }

    private string GetAssertionsSummary(List<AssertionResult> assertions)
    {
        var passed = assertions.Count(a => a.Success);
        var failed = assertions.Count(a => !a.Success);
        
        if (failed > 0)
        {
            return $"{passed} passed, {failed} failed";
        }
        
        return $"{passed} passed";
    }

    private string FormatVariableValue(object value, string variableKey = "")
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value.GetType().IsValueType) return value.ToString() ?? "null";
        
        return FormatComplexValue(value, variableKey);
    }

    private string FormatVariableValueForTable(object value, string variableKey = "")
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value.GetType().IsValueType) return value.ToString() ?? "null";
        
        return FormatComplexValueForTable(value, variableKey);
    }
    
    private string FormatComplexValue(object value, string variableKey = "")
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            };
            
            var json = JsonSerializer.Serialize(value, options);
            
            // Don't register complex JSON for masking here - it will be handled by ApplyMasking at the end
            return $"\n\n<details><summary>show</summary>\n\n```json\n{json}\n```\n</details>\n\n";

        }
        catch
        {
            return value.ToString() ?? "null";
        }
    }

    private string FormatComplexValueForTable(object value, string variableKey = "")
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,  // Use compact JSON for table cells to avoid linebreaks
                PropertyNamingPolicy = null
            };
            
            var json = JsonSerializer.Serialize(value, options);
            
            // For tables, use a compact collapsible format that fits in a single table cell
            return $"<details><summary>show JSON</summary><pre>{json}</pre></details>";

        }
        catch
        {
            return value.ToString() ?? "null";
        }
    }

    private string FormatVariableValueForHtmlTable(object value, string variableKey = "")
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value.GetType().IsValueType) return value.ToString() ?? "null";
        
        return FormatComplexValueForHtmlTable(value, variableKey);
    }
    
    private string FormatComplexValueForHtmlTable(object value, string variableKey = "")
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,  // Use proper indented JSON for HTML tables
                PropertyNamingPolicy = null
            };
            
            var json = JsonSerializer.Serialize(value, options);
            
            // HTML-encode the JSON to prevent issues with HTML parsing
            json = System.Net.WebUtility.HtmlEncode(json);
            
            // For HTML tables, use proper indented format with details/summary for collapsible content
            return $"<details><summary>show JSON</summary><pre>{json}</pre></details>";

        }
        catch
        {
            return System.Net.WebUtility.HtmlEncode(value.ToString() ?? "null");
        }
    }
}

// Helper class for HTTP request details
internal class HttpRequestDetails
{
    public string? Url { get; set; }
    public string? Method { get; set; }
    public object[]? Headers { get; set; }
    public string? Body { get; set; }
}