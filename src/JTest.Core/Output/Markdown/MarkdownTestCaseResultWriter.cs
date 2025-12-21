using JTest.Core.Assertions;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using System.Text.Json;

namespace JTest.Core.Output.Markdown;

public sealed class MarkdownTestCaseResultWriter : ITestCaseResultWriter
{
    private readonly HttpStepResultDataWriter httpStepResultDataWriter = new();

    public void Write(TextWriter writer, JTestCaseResult testCaseResult, bool isDebug)
    {
        if (testCaseResult.Dataset is not null)
        {
            writer.WriteLine($"## Test case (dataset): {testCaseResult.Dataset.Name}");
        }
        else
        {
            writer.WriteLine($"## Test Case: {testCaseResult.TestCaseName}");
        }
        writer.WriteLine();

        if (testCaseResult.Success)
        {
            writer.WriteLine("**Status**: SUCCESS <br/>");
        }
        else
        {
            writer.WriteLine($"**Status**: FAILED <br/>");
            if (!string.IsNullOrWhiteSpace(testCaseResult.ErrorMessage))
            {
                writer.WriteLine($"**Error**: '{testCaseResult.ErrorMessage}' <br/>");
            }
        }

        writer.WriteLine($"**Duration**: {testCaseResult.DurationMs} ms <br/>");

        writer.WriteLine();
        writer.WriteLine("### Step Results:");
        writer.WriteLine();

        foreach (var item in testCaseResult.StepResults)
        {

            WriteStepResult(writer, item, isDebug, null);
        }
        writer.WriteLine();
        writer.WriteLine("---");
        writer.WriteLine();
    }

    private void WriteStepResult(TextWriter writer, StepResult stepResult, bool isDebug, string? templateName)
    {
        var showStepNumber = string.IsNullOrWhiteSpace(templateName);
        if (!string.IsNullOrWhiteSpace(stepResult.Step.Configuration.Name))
        {
            if (showStepNumber)
            {
                writer.WriteLine($"**Step {stepResult.StepNumber}**: {stepResult.Step.Configuration.Name} <br/>");
                writer.WriteLine($"**Step type:** {stepResult.Step.TypeName} <br/>");
            }
            else
            {
                writer.WriteLine($"**Step for template {templateName}:** {stepResult.Step.Configuration.Name} <br/>");
                writer.WriteLine($"**Step type:** {stepResult.Step.TypeName} <br/>");
            }
        }
        else if (showStepNumber)
        {
            writer.WriteLine($"**Step {stepResult.StepNumber}**: {stepResult.Step.TypeName} <br/>");
        }
        else
        {
            writer.WriteLine($"**Step for template {templateName}**: {stepResult.Step.TypeName} <br/>");
        }

        var useStepConfig = stepResult.Step.Configuration as UseStepConfiguration;
        if (useStepConfig is not null)
        {
            writer.WriteLine($"**Template**: {useStepConfig.Template}  <br/>");
        }

        if (!string.IsNullOrWhiteSpace(stepResult.Step.Configuration.Description))
        {
            writer.WriteLine($"**Description**: {stepResult.Step.Configuration.Description} <br/>");
        }


        var status = stepResult.Success ? "SUCCESS" : "FAILED";
        writer.WriteLine($"**Status**: {status} <br/>");
        if (!stepResult.Success && !string.IsNullOrWhiteSpace(stepResult.ErrorMessage))
        {
            writer.WriteLine($"**Error**: '{stepResult.ErrorMessage}' <br/>");
        }

        writer.WriteLine();

        if (isDebug && stepResult.Step is HttpStep)
        {
            httpStepResultDataWriter.WriteData(writer, stepResult);
            writer.WriteLine();
        }

        writer.WriteLine();

        if (stepResult.AssertionResults.Any())
        {
            writer.WriteLine();
            writer.WriteLine("**Assertions:** <br/>");
            writer.WriteLine();

            WriteAssertionsTable(writer, stepResult.AssertionResults);
        }

        if (stepResult.InnerResults.Any())
        {
            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine($"**Inner step results for template**: '{useStepConfig?.Template}' <br/>");
            writer.WriteLine();

            foreach (var innerStepResult in stepResult.InnerResults)
            {
                WriteStepResult(writer, innerStepResult, isDebug, useStepConfig?.Template);
            }
        }
    }

    private void WriteAssertionsTable(TextWriter writer, IEnumerable<AssertionResult> assertions)
    {
        writer.WriteLine("<table style=\"border-collapse: collapse;\">");
        writer.WriteLine("<thead>");
        writer.WriteLine("<tr>");
        WriteAsserionTableHeader(writer, "Result");
        WriteAsserionTableHeader(writer, "Operator");
        WriteAsserionTableHeader(writer, "Actual");
        WriteAsserionTableHeader(writer, "Expected");
        WriteAsserionTableHeader(writer, "Description/Error");
        writer.WriteLine("</tr>");
        writer.WriteLine("</thead>");

        writer.WriteLine("<tbody>");

        foreach (var assertion in assertions)
        {
            writer.WriteLine("<tr>");

            var result = assertion.Success ? "PASS" : "FAILED";
            var actualValue = ParseObjectValue(assertion.ActualValue, assertion.MaskValue);
            var expectedValue = ParseObjectValue(assertion.ExpectedValue, false);

            WriteAssertionColumn(writer, result);
            WriteAssertionColumn(writer, assertion.Operation);
            WriteAssertionColumn(writer, actualValue);
            WriteAssertionColumn(writer, expectedValue);
            if (assertion.Success)
            {
                var description = !string.IsNullOrWhiteSpace(assertion.Description)
                    ? $"<details><summary>show description</summary><pre>{assertion.Description}</pre></details>"
                    : string.Empty;

                WriteAssertionColumn(writer, description);
            }
            else
            {
                WriteAssertionColumn(writer, assertion.ErrorMessage);
            }

            writer.WriteLine("</tr>");
        }

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table>");
        writer.WriteLine("<br/>");
        writer.WriteLine();
    }

    private static void WriteAssertionColumn(TextWriter writer, string value)
    {
        writer.WriteLine("<td style=\"border: 1px solid #333; padding: 8px; word-wrap: break-word; white-space: normal;\">");
        writer.WriteLine(value);
        writer.WriteLine("</td>");
    }

    private static void WriteAsserionTableHeader(TextWriter writer, string header)
    {
        writer.WriteLine($"<th style=\"border: 1px solid #333; padding: 8px; width: 150px;\">{header}</th>");

    }

    private static string ParseObjectValue(object? value, bool maskValue)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string result;
        if (value is JsonElement jsonElement)
        {
            result = jsonElement.GetRawText();
        }

        else if (value.GetType().IsPrimitive || value.GetType() == typeof(string))
        {
            result = value.ToString() ?? string.Empty;
        }

        else
        {
            var json = JsonSerializer.Serialize(value);
            result = $"<details><summary>show JSON</summary><pre>{json}</pre></details>";
        }

        if (maskValue)
        {
            return "masked";
        }

        return result;
    }
}
