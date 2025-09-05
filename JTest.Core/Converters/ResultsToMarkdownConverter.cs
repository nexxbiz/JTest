using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Assertions;
using JTest.Core.Debugging;
using System.Text;
using System.Text.Json;

namespace JTest.Core.Converters;

public class ResultsToMarkdownConverter
{
    public string ConvertToMarkdown(List<JTestCaseResult> results)
    {
        var content = new StringBuilder();
        content.AppendLine();
        
        foreach (var result in results)
        {
            AppendTestCaseResult(content, result);
        }
        
        return content.ToString();
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
            content.AppendLine(step.DetailedDescription);
        }
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            content.AppendLine($"  - **Error:** {step.ErrorMessage}");
        }
        
        AppendSavedValues(content, step.ContextChanges);
        AppendAssertionResults(content, step.AssertionResults);
        AppendInnerSteps(content, step.InnerResults);
    }

    private void AppendAssertionResults(StringBuilder content, List<AssertionResult> assertions)
    {
        if (assertions.Count == 0) return;
        
        content.AppendLine("  - **Assertions:**");
        
        foreach (var assertion in assertions)
        {
            AppendSingleAssertion(content, assertion);
        }
    }

    private void AppendSingleAssertion(StringBuilder content, AssertionResult assertion)
    {
        var status = assertion.Success ? "PASSED" : "FAILED";
        var description = string.IsNullOrEmpty(assertion.Description) 
            ? assertion.Operation 
            : assertion.Description;
            
        content.AppendLine($"    - {status}: {description}");
        
        if (!assertion.Success)
        {
            AppendAssertionDetails(content, assertion);
        }
    }

    private void AppendAssertionDetails(StringBuilder content, AssertionResult assertion)
    {
        if (assertion.ActualValue != null)
        {
            content.AppendLine($"      - **Actual:** {assertion.ActualValue}");
        }
        
        if (assertion.ExpectedValue != null)
        {
            content.AppendLine($"      - **Expected:** {assertion.ExpectedValue}");
        }
        
        if (!string.IsNullOrEmpty(assertion.ErrorMessage))
        {
            content.AppendLine($"      - **Error:** {assertion.ErrorMessage}");
        }
    }

    private void AppendSavedValues(StringBuilder content, ContextChanges? contextChanges)
    {
        if (contextChanges == null) return;
        if (contextChanges.Added.Count == 0 && contextChanges.Modified.Count == 0) return;
        
        content.AppendLine("  - **Saved Values:**");
        AppendSavedVariables(content, contextChanges.Added, "Added");
        AppendSavedVariables(content, contextChanges.Modified, "Modified");
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
        var valueDisplay = FormatVariableValue(variable.Value);
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
    }

    private void AppendInnerStepResult(StringBuilder content, StepResult step)
    {
        var status = step.Success ? "PASSED" : "FAILED";
        var description = GetInnerStepDescription(step);
        content.AppendLine($"    - **{description}:** {status} ({step.DurationMs}ms)");
        
        AppendInnerStepDetails(content, step);
    }

    private string GetInnerStepDescription(StepResult step)
    {
        return !string.IsNullOrEmpty(step.DetailedDescription) 
            ? step.DetailedDescription 
            : $"{step.Step.Type} step";
    }

    private void AppendInnerStepDetails(StringBuilder content, StepResult step)
    {
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            content.AppendLine($"      - **Error:** {step.ErrorMessage}");
        }
        
        AppendInnerSavedValues(content, step.ContextChanges);
        AppendInnerAssertionResults(content, step.AssertionResults);
    }

    private void AppendInnerSavedValues(StringBuilder content, ContextChanges? contextChanges)
    {
        if (contextChanges == null) return;
        if (contextChanges.Added.Count == 0 && contextChanges.Modified.Count == 0) return;
        
        content.AppendLine("      - **Saved Values:**");
        AppendInnerSavedVariables(content, contextChanges.Added, "Added");
        AppendInnerSavedVariables(content, contextChanges.Modified, "Modified");
    }

    private void AppendInnerSavedVariables(StringBuilder content, Dictionary<string, object> variables, string category)
    {
        if (variables.Count == 0) return;
        
        foreach (var variable in variables)
        {
            if (ShouldSkipVariable(variable.Key)) continue;
            AppendInnerSavedVariable(content, variable, category);
        }
    }

    private void AppendInnerSavedVariable(StringBuilder content, KeyValuePair<string, object> variable, string category)
    {
        var valueDisplay = FormatVariableValue(variable.Value);
        content.AppendLine($"        - **{category}:** {variable.Key} = {valueDisplay}");
    }

    private void AppendInnerAssertionResults(StringBuilder content, List<AssertionResult> assertions)
    {
        if (assertions.Count == 0) return;
        
        content.AppendLine("      - **Assertions:**");
        foreach (var assertion in assertions)
        {
            AppendInnerAssertion(content, assertion);
        }
    }

    private void AppendInnerAssertion(StringBuilder content, AssertionResult assertion)
    {
        var status = assertion.Success ? "PASSED" : "FAILED";
        var description = string.IsNullOrEmpty(assertion.Description) 
            ? assertion.Operation 
            : assertion.Description;
            
        content.AppendLine($"        - {status}: {description}");
        
        if (!assertion.Success)
        {
            AppendInnerAssertionDetails(content, assertion);
        }
    }

    private void AppendInnerAssertionDetails(StringBuilder content, AssertionResult assertion)
    {
        if (assertion.ActualValue != null)
        {
            content.AppendLine($"          - **Actual:** {assertion.ActualValue}");
        }
        
        if (assertion.ExpectedValue != null)
        {
            content.AppendLine($"          - **Expected:** {assertion.ExpectedValue}");
        }
        
        if (!string.IsNullOrEmpty(assertion.ErrorMessage))
        {
            content.AppendLine($"          - **Error:** {assertion.ErrorMessage}");
        }
    }

    private string FormatVariableValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value.GetType().IsValueType) return value.ToString() ?? "null";
        
        return FormatComplexValue(value);
    }
    
    private string FormatComplexValue(object value)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            };
            
            var json = JsonSerializer.Serialize(value, options);
            return $"\n<details><summary>show</summary>\n\n```json\n{json}\n```\n</details>\n";


        }
        catch
        {
            return value.ToString() ?? "null";
        }
    }
}