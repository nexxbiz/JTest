using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Assertions;
using System.Text;

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
        
        if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
        {
            content.AppendLine($"  - **Error:** {step.ErrorMessage}");
        }
        
        AppendAssertionResults(content, step.AssertionResults);
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
}