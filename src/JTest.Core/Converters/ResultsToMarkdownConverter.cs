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
            content.AppendLine($"  {step.DetailedDescription}");
        }
        
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            _securityMasker.RegisterForMasking("error", step.ErrorMessage);
            content.AppendLine($"  - **Error:** {step.ErrorMessage}");
        }
        
        AppendSavedValues(content, step.ContextChanges);
        AppendAssertionResults(content, step.AssertionResults);
        AppendInnerSteps(content, step.InnerResults);
        
        content.AppendLine();
    }

    private void AppendAssertionResults(StringBuilder content, List<AssertionResult> assertions)
    {
        if (assertions.Count == 0) return;
        
        content.AppendLine("  - **Assertions:**");
        
        foreach (var assertion in assertions)
        {
            AppendSingleAssertion(content, assertion);
        }
        
        content.AppendLine();
    }

    private void AppendSingleAssertion(StringBuilder content, AssertionResult assertion)
    {
        var status = assertion.Success ? "PASSED" : "FAILED";
        var description = string.IsNullOrEmpty(assertion.Description) 
            ? assertion.Operation 
            : assertion.Description;
            
        content.AppendLine($"    - {status}: {description}");
        
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
            content.AppendLine($"      {string.Join(", \n", details)}\n");
        }
    }

    private void AppendSavedValues(StringBuilder content, ContextChanges? contextChanges)
    {
        if (contextChanges == null) return;
        if (contextChanges.Added.Count == 0 && contextChanges.Modified.Count == 0) return;
        
        content.AppendLine("- **Saved Values:**");
        AppendSavedVariables(content, contextChanges.Added, "Added");
        AppendSavedVariables(content, contextChanges.Modified, "Modified");
        
        content.AppendLine();
    }

    private void AppendSavedVariables(StringBuilder content, Dictionary<string, object> variables, string category)
    {
        if (variables.Count == 0) return;
        
        foreach (var variable in variables)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            AppendSingleSavedVariable(content, variable, category);
        }
    }

    private bool ShouldSkipVariable(string variableName)
    {
        // Skip internal variables that are not user-created
        return variableName.Equals("this", StringComparison.OrdinalIgnoreCase);
    }

    private void AppendSingleSavedVariable(StringBuilder content, KeyValuePair<string, object> variable, string category)
    {
        // Register sensitive values for masking
        _securityMasker.RegisterForMasking(variable.Key, variable.Value);
        
        var valueDisplay = FormatVariableValue(variable.Value, variable.Key);
        content.AppendLine($"    - **{category}:** {variable.Key} = {valueDisplay}");
    }

    private void AppendInnerSteps(StringBuilder content, List<StepResult> innerResults)
    {
        if (innerResults.Count == 0) return;
        
        content.AppendLine("  - **Template Steps:**");
        foreach (var innerStep in innerResults)
        {
            AppendInnerStepResult(content, innerStep);
        }
        
        content.AppendLine();
    }

    private void AppendInnerStepResult(StringBuilder content, StepResult step)
    {
        var status = step.Success ? "PASSED" : "FAILED";
        var description = GetInnerStepDescription(step);
        content.AppendLine($"  - **{description}** {status} ({step.DurationMs}ms)");
        
        AppendInnerStepDetails(content, step);
    }

    private string GetInnerStepDescription(StepResult stepResult)
    {
       
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
            content.AppendLine($"      ({string.Join(", ", details)})");
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
}