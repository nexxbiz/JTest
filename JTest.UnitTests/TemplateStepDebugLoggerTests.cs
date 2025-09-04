using JTest.Core.Debugging;
using JTest.Core.Assertions;
using Xunit;

namespace JTest.UnitTests
{
    public class TemplateStepDebugLoggerTests
    {
        [Fact]
        public void TemplateStepDebugLogger_GetOutput_IncludesAssertionResults()
        {
            // Arrange
            var logger = new TemplateStepDebugLogger();
            
            // Simulate step execution and assertion logging
            var stepInfo = new StepDebugInfo
            {
                TestNumber = 1,
                StepNumber = 1,
                StepType = "HttpStep",
                StepId = "test-step",
                Enabled = true,
                Result = "Success",
                Duration = TimeSpan.FromMilliseconds(100)
            };
            
            var assertionResults = new List<AssertionResult>
            {
                new AssertionResult(true)
                {
                    Operation = "equals",
                    Description = "Status code should be 200",
                    ActualValue = 200,
                    ExpectedValue = 200
                },
                new AssertionResult(false, "Value is missing")
                {
                    Operation = "exists",
                    Description = "Required field should exist",
                    ActualValue = null
                }
            };
            
            // Act
            logger.LogStepExecution(stepInfo);
            logger.LogAssertionResults(assertionResults);
            var output = logger.GetOutput();
            
            // Assert - Should include comprehensive assertion details using MarkdownDebugLogger formatting
            Assert.Contains("**Template Step Execution Details:**", output);
            Assert.Contains("**HttpStep** (test-step): Success", output);
            Assert.Contains("**Assertions:**", output);
            Assert.Contains("**Assert Name:** equals", output);
            Assert.Contains("**Status:** PASSED ✅", output);
            Assert.Contains("**Status:** FAILED ❌", output);
            Assert.Contains("**Error:** Value is missing", output);
        }
    }
}