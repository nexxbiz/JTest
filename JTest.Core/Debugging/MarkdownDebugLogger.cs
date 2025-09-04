using System.Globalization;
using System.Text;
using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Models;

namespace JTest.Core.Debugging;

/// <summary>
/// Markdown debug logger implementation that generates formatted debug output
/// </summary>
public class MarkdownDebugLogger : IDebugLogger
{
    private readonly StringBuilder _output = new();
    private readonly SecurityMasker _securityMasker = new();
    private bool _headerWritten = false;
    private readonly List<AssertionResult> _allAssertionResults = new();
    
    // Store current test context information for assertion logging
    private string _currentTestName = string.Empty;
    private string _currentTestCaseName = string.Empty;
    private int _currentTestNumber = 1;
    private int _currentStepNumber = 1;

    public void LogStepExecution(StepDebugInfo stepInfo)
    {
        // Store current test context for assertion logging
        _currentTestName = stepInfo.TestName;
        _currentTestCaseName = stepInfo.TestName; // For now, using TestName as TestCaseName
        _currentTestNumber = stepInfo.TestNumber;
        _currentStepNumber = stepInfo.StepNumber;
        
        // Write header only once per debug session
        if (!_headerWritten)
        {
            WriteDebugReportHeader(stepInfo);
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
        // Note: No longer showing JSONPath clutter as per requirements
    }

    public void LogRuntimeContext(Dictionary<string, object> context)
    {
        // Note: No longer showing runtime context as per requirements
        // This method is now a no-op
    }

    public void LogAssertionResults(List<AssertionResult> assertionResults)
    {
        _allAssertionResults.AddRange(assertionResults);
        WriteAssertionResults(assertionResults);
    }

    public void LogTestSummary(List<JTestCaseResult> testResults)
    {
        if (!testResults.Any()) return;
        
        // Store current test case name from the first result for assertion context
        if (testResults.Count > 0)
        {
            _currentTestCaseName = testResults[0].TestCaseName;
        }

        _output.AppendLine();
        _output.AppendLine("---");
        _output.AppendLine();
        _output.AppendLine("# Test Execution Summary");
        _output.AppendLine();

        var totalTests = testResults.Count;
        var successfulTests = testResults.Count(r => r.Success);
        var failedTests = totalTests - successfulTests;
        
        // Overall statistics
        _output.AppendLine($"**Total Tests:** {totalTests}");
        _output.AppendLine($"**Successful:** {successfulTests} ✅");
        _output.AppendLine($"**Failed:** {failedTests} ❌");
        _output.AppendLine();

        // Collect all failed assertions across all tests
        var failedAssertions = new List<(string testName, AssertionResult assertion)>();
        
        foreach (var testResult in testResults)
        {
            foreach (var stepResult in testResult.StepResults)
            {
                if (stepResult.AssertionResults?.Any() == true)
                {
                    var failedStepAssertions = stepResult.AssertionResults.Where(a => !a.Success);
                    foreach (var assertion in failedStepAssertions)
                    {
                        failedAssertions.Add((testResult.TestCaseName, assertion));
                    }
                }
            }
        }

        // Show detailed information for each test including all assertions
        if (testResults.Any())
        {
            _output.AppendLine("## Test Details");
            _output.AppendLine();
            
            foreach (var testResult in testResults)
            {
                var status = testResult.Success ? "✅ PASSED" : "❌ FAILED";
                _output.AppendLine($"### {testResult.TestCaseName} - {status}");
                _output.AppendLine();
                
                // Show all assertions for this test
                var allTestAssertions = new List<AssertionResult>();
                foreach (var stepResult in testResult.StepResults)
                {
                    if (stepResult.AssertionResults?.Any() == true)
                    {
                        allTestAssertions.AddRange(stepResult.AssertionResults);
                    }
                }
                
                if (allTestAssertions.Any())
                {
                    _output.AppendLine("**Assertions:**");
                    foreach (var assertion in allTestAssertions)
                    {
                        var description = assertion.Description ?? $"{assertion.Operation} assertion";
                        var actualDisplay = FormatAssertionValue(assertion.ActualValue ?? "null");
                        var statusIcon = assertion.Success ? "✅" : "❌";
                        var statusText = assertion.Success ? "PASSED" : "FAILED";
                        
                        if (assertion.Success)
                        {
                            _output.AppendLine($"- **{testResult.TestCaseName}** > {description} : {statusText} {statusIcon}");
                        }
                        else
                        {
                            if (assertion.ExpectedValue != null)
                            {
                                var expectedDisplay = FormatAssertionValue(assertion.ExpectedValue);
                                _output.AppendLine($"- **{testResult.TestCaseName}** > {description} : got `{actualDisplay}` : expected `{expectedDisplay}` : {statusText} {statusIcon}");
                            }
                            else
                            {
                                _output.AppendLine($"- **{testResult.TestCaseName}** > {description} : got `{actualDisplay}` : {statusText} {statusIcon}");
                            }
                            
                            if (!string.IsNullOrEmpty(assertion.ErrorMessage))
                            {
                                _output.AppendLine($"  - **Error:** {assertion.ErrorMessage}");
                            }
                        }
                    }
                    _output.AppendLine();
                }
                else if (!testResult.Success)
                {
                    _output.AppendLine("**Error Details:**");
                    if (!string.IsNullOrEmpty(testResult.ErrorMessage))
                    {
                        _output.AppendLine($"- {testResult.ErrorMessage}");
                    }
                    else
                    {
                        _output.AppendLine($"- Test failed without specific assertion errors");
                    }
                    _output.AppendLine();
                }
            }
        }
        
        // Show summary of failed assertions if any exist
        if (failedAssertions.Any())
        {
            _output.AppendLine("## Failed Assertions Summary");
            _output.AppendLine();
            
            foreach (var (testName, assertion) in failedAssertions)
            {
                var description = assertion.Description ?? $"{assertion.Operation} assertion";
                var actualDisplay = FormatAssertionValue(assertion.ActualValue ?? "null");
                var expectedDisplay = FormatAssertionValue(assertion.ExpectedValue ?? "null");
                
                _output.AppendLine($"**Test:** {testName}");
                _output.AppendLine($"**TestCase:** {testName}");
                
                if (assertion.ExpectedValue != null)
                {
                    _output.AppendLine($"**Assert:** {description} : got `{actualDisplay}` : expected `{expectedDisplay}` ❌");
                }
                else
                {
                    _output.AppendLine($"**Assert:** {description} : got `{actualDisplay}` ❌");
                }
                
                if (!string.IsNullOrEmpty(assertion.ErrorMessage))
                {
                    _output.AppendLine($"**Error:** {assertion.ErrorMessage}");
                }
                _output.AppendLine();
            }
        }

        if (failedTests == 0)
        {
            _output.AppendLine("🎉 **All tests passed successfully!**");
            _output.AppendLine();
        }
    }

    public string GetOutput() 
    {
        var rawOutput = _output.ToString();
        return _securityMasker.ApplyMasking(rawOutput);
    }

    private void WriteDebugReportHeader(StepDebugInfo stepInfo)
    {
        var fileName = !string.IsNullOrEmpty(stepInfo.TestFileName) ? stepInfo.TestFileName : "test-file.json";
        
        _output.AppendLine($"# Debug Report for {fileName}");
        _output.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.AppendLine($"**Test File:** {fileName}");
        
        // Add test suite information if available
        if (!string.IsNullOrEmpty(stepInfo.TestSuiteName))
        {
            _output.AppendLine($"**Test Suite:** {stepInfo.TestSuiteName}");
        }
        
        if (!string.IsNullOrEmpty(stepInfo.TestSuiteDescription))
        {
            _output.AppendLine($"**Suite Description:** {stepInfo.TestSuiteDescription}");
        }
        
        _output.AppendLine();
        _output.AppendLine("---");
        _output.AppendLine();
    }

    private void WriteTestStepHeader(StepDebugInfo stepInfo)
    {
        _output.AppendLine();
        
        // Enhanced test identification with names and descriptions
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

        // Enhanced description with test context information
        var description = result.Description ?? $"{result.Operation} assertion";
        
        // Include comprehensive details as requested in the problem statement
        _output.AppendLine($"**Test:** {_currentTestCaseName}");
        _output.AppendLine($"**Test Case:** {_currentTestCaseName}");
        _output.AppendLine($"**Assert Name:** {result.Operation}");
        _output.AppendLine($"**Description:** {description}");
        _output.AppendLine($"**Status:** {status} {statusIcon}");
        
        if (result.ActualValue != null)
        {
            var actualDisplay = FormatAssertionValue(result.ActualValue);
            _output.AppendLine($"**Actual Value:** `{actualDisplay}`");
        }
        
        if (result.ExpectedValue != null)
        {
            var expectedDisplay = FormatAssertionValue(result.ExpectedValue);
            _output.AppendLine($"**Expected Value:** `{expectedDisplay}`");
        }
        
        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            _output.AppendLine($"**Error:** {result.ErrorMessage}");
        }
        
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
            var maskedValue = _securityMasker.RegisterForMasking(param.Key, param.Value);
            var valueDesc = DescribeValue(maskedValue);
            _output.AppendLine($"- `{param.Key}`: {valueDesc}");
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
        _output.AppendLine("**Variables Saved:**");
        foreach (var saved in savedVariables)
        {
            WriteSavedVariableWithDetails(saved.Key, saved.Value);
        }
        _output.AppendLine();
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
        var jsonContent = FormatObjectAsJson(value);
        // Fix indentation by ensuring all lines are properly indented
        var lines = jsonContent.Split('\n');
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _output.AppendLine($"  {line}");
            }
            else
            {
                _output.AppendLine();
            }
        }
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
            var maskedValue = _securityMasker.RegisterForMasking(key, str);
            _output.AppendLine($"- `{key}`: {DescribeValue(maskedValue)}");
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
            var jsonContent = FormatObjectAsJson(value);
            // Fix indentation by ensuring all lines are properly indented  
            var lines = jsonContent.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _output.AppendLine($"  {line}");
                }
                else
                {
                    _output.AppendLine();
                }
            }
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
            var jsonContent = FormatObjectAsJson(value);
            var lines = jsonContent.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _output.AppendLine($"  {line}");
                }
                else
                {
                    _output.AppendLine();
                }
            }
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
            var jsonContent = FormatObjectAsJson(value);
            var lines = jsonContent.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _output.AppendLine($"  {line}");
                }
                else
                {
                    _output.AppendLine();
                }
            }
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
            var jsonContent = FormatObjectAsJson(value);
            var lines = jsonContent.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _output.AppendLine($"  {line}");
                }
                else
                {
                    _output.AppendLine();
                }
            }
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