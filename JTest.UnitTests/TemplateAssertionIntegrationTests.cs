using JTest.Core.Debugging;
using JTest.Core.Models;
using JTest.Core.Assertions;
using Xunit;

namespace JTest.UnitTests
{
    public class TemplateAssertionIntegrationTests
    {
        [Fact]
        public void TemplateStepWithAssertions_ShowsAssertionsInDebugOutput()
        {
            // Arrange - Set up a template step with assertions
            var debugLogger = new MarkdownDebugLogger();
            var templateDebugLogger = new TemplateStepDebugLogger();
            
            // Simulate a template step (HttpStep) execution with assertions
            var httpStepInfo = new StepDebugInfo
            {
                TestNumber = 1,
                StepNumber = 1,
                StepType = "HttpStep",
                StepId = "api-call",
                Enabled = true,
                Result = "Success",
                Duration = TimeSpan.FromMilliseconds(150),
                TestName = "API Integration Test"
            };
            
            var httpAssertions = new List<AssertionResult>
            {
                new AssertionResult(true)
                {
                    Operation = "equals",
                    Description = "Status code should be 200",
                    ActualValue = 200,
                    ExpectedValue = 200
                },
                new AssertionResult(false, "Required header missing")
                {
                    Operation = "exists",
                    Description = "Content-Type header should exist",
                    ActualValue = null
                }
            };
            
            // Simulate another template step (AssertStep) with assertions
            var assertStepInfo = new StepDebugInfo
            {
                TestNumber = 1,
                StepNumber = 2,
                StepType = "AssertStep",
                StepId = "validate-response",
                Enabled = true,
                Result = "Failed",
                Duration = TimeSpan.FromMilliseconds(25),
                TestName = "API Integration Test"
            };
            
            var assertStepAssertions = new List<AssertionResult>
            {
                new AssertionResult(false, "Value mismatch")
                {
                    Operation = "equals",
                    Description = "Response body should contain expected data",
                    ActualValue = "invalid",
                    ExpectedValue = "valid"
                }
            };
            
            // Act - Simulate template step execution and logging
            templateDebugLogger.LogStepExecution(httpStepInfo);
            templateDebugLogger.LogAssertionResults(httpAssertions);
            
            templateDebugLogger.LogStepExecution(assertStepInfo);
            templateDebugLogger.LogAssertionResults(assertStepAssertions);
            
            // Simulate UseStep logging template execution details
            var useStepInfo = new StepDebugInfo
            {
                TestNumber = 1,
                StepNumber = 1,
                StepType = "UseStep",
                StepId = "execute-template",
                Enabled = true,
                Result = "Failed",
                Duration = TimeSpan.FromMilliseconds(200),
                TestName = "Template Execution Test",
                TemplateExecution = new TemplateExecutionInfo
                {
                    TemplateName = "api-validation-template",
                    InputParameters = new Dictionary<string, object> { ["apiUrl"] = "https://api.example.com" },
                    StepsExecuted = 2,
                    OutputValues = new Dictionary<string, object> { ["status"] = "failed" },
                    SavedVariables = new Dictionary<string, object> { ["errorDetails"] = "validation failed" },
                    StepExecutionDetails = templateDebugLogger.GetCapturedSteps()
                }
            };
            
            debugLogger.LogStepExecution(useStepInfo);
            var output = debugLogger.GetOutput();
            
            // Assert - Verify that template step assertions are included in the debug output
            Assert.Contains("Template Execution Details (Click to expand)", output);
            Assert.Contains("**Step Execution Details:**", output);
            Assert.Contains("**HttpStep** (api-call): Success", output);
            Assert.Contains("**AssertStep** (validate-response): Failed", output);
            
            // Verify that assertion details are shown for each template step
            Assert.Contains("**Assertions:**", output);
            Assert.Contains("**Assert Name:** equals", output);
            Assert.Contains("**Status:** PASSED ✅", output);
            Assert.Contains("**Status:** FAILED ❌", output);
            Assert.Contains("**Error:** Required header missing", output);
            Assert.Contains("**Error:** Value mismatch", output);
        }
    }
}