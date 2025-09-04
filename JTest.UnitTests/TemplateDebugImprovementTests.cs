using JTest.Core.Debugging;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.UnitTests;

public class TemplateDebugImprovementTests
{
    [Fact]
    public void MarkdownDebugLogger_SavedVariables_ShouldShowDetailedContent()
    {
        // Arrange
        var logger = new MarkdownDebugLogger();
        var templateInfo = new TemplateExecutionInfo
        {
            TemplateName = "execute-workflow-and-get-activity",
            StepsExecuted = 2,
            InputParameters = new Dictionary<string, object>
            {
                ["username"] = "testuser",
                ["password"] = "secret123",
                ["tokenUrl"] = "https://api.example.com/token"
            },
            SavedVariables = new Dictionary<string, object>
            {
                ["ifConditionResult"] = new Dictionary<string, object>
                {
                    ["activityExecution"] = new Dictionary<string, object>
                    {
                        ["activityState"] = new Dictionary<string, object>
                        {
                            ["Condition"] = false
                        }
                    },
                    ["workflowInstanceId"] = "b961ee5b6f66b01c",
                    ["status"] = "completed",
                    ["data"] = new { result = "success" },
                    ["metadata"] = new Dictionary<string, object>
                    {
                        ["processedAt"] = "2024-01-01T10:00:00Z",
                        ["duration"] = 1500
                    }
                }
            }
        };

        var stepInfo = new StepDebugInfo
        {
            TestNumber = 1,
            StepNumber = 1,
            StepType = "UseStep",
            StepId = "execute-workflow-test",
            Enabled = true,
            Result = "Success",
            Duration = TimeSpan.FromMilliseconds(500),
            TestFileName = "set-variable-tests.json",
            TestName = "Get tokens",
            TestDescription = "Authenticate and store credentials globally",
            TemplateExecution = templateInfo
        };

        // Act
        logger.LogStepExecution(stepInfo);
        var output = logger.GetOutput();

        // Print the actual output for debugging
        Console.WriteLine("=== ACTUAL OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");

        // Assert - Should have improved header and formatting
        Assert.Contains("Debug Report for set-variable-tests.json", output);
        Assert.Contains("Test 1 - Get tokens", output);
        Assert.Contains("Authenticate and store credentials globally", output);
        Assert.Contains("**Step:** use execute-workflow-and-get-activity", output); // Fixed formatting
        Assert.Contains("**Input Parameters:**", output);
        Assert.Contains("\"masked\"", output); // Security masking (password should be masked)
        Assert.Contains("Saved variables:", output); // Updated to lowercase
        Assert.Contains("ifConditionResult", output);
        
        // Current behavior shows truncated info - we want to change this
        // This test documents the current behavior we want to improve
        Console.WriteLine("Enhanced output:");
        Console.WriteLine(output);
    }

    [Fact]
    public void MarkdownDebugLogger_AssertionResults_ShouldIncludeTestScenarioContext()
    {
        // Arrange
        var logger = new MarkdownDebugLogger();
        var context = new TestExecutionContext();
        context.SetCase(new Dictionary<string, object>
        {
            ["testScenario"] = "false condition path",
            ["expectedCondition"] = false,
            ["isTrue"] = false
        });

        var assertionResults = new List<AssertionResult>
        {
            new AssertionResult(true)
            {
                Operation = "equals",
                Description = "Activity condition should match expected value for false condition path",
                ActualValue = false,
                ExpectedValue = false
            },
            new AssertionResult(false)
            {
                Operation = "exists", 
                Description = "Workflow instance ID should exist for false condition path",
                ActualValue = null,
                ErrorMessage = "Value is null"
            }
        };

        // Act
        logger.LogAssertionResults(assertionResults);
        var output = logger.GetOutput();

        // Assert - Should show improved assertion format
        Assert.Contains("**Assertions:**", output);
        Assert.Contains("Activity condition should match expected value for false condition path : PASSED ✅", output);
        Assert.Contains("Workflow instance ID should exist for false condition path : got `null` : FAILED ❌", output);
        Assert.Contains("false condition path", output);
        
        Console.WriteLine("Improved assertion output:");
        Console.WriteLine(output);
    }

    [Fact]
    public void TemplateExecution_ShowsDetailedSavedVariables_WithTestScenarioContext()
    {
        // Arrange - Simulate the exact scenario from the problem statement
        var logger = new MarkdownDebugLogger();
        
        // Create data that matches the problem statement example
        var ifConditionResult = new Dictionary<string, object>
        {
            ["activityExecution"] = new Dictionary<string, object>
            {
                ["activityState"] = new Dictionary<string, object>
                {
                    ["Condition"] = false
                }
            },
            ["workflowInstanceId"] = "b961ee5b6f66b01c",
            ["status"] = "completed",
            ["metadata"] = new Dictionary<string, object>
            {
                ["processedAt"] = "2024-01-01T10:00:00Z",
                ["duration"] = 1500,
                ["testScenario"] = "false condition path"
            },
            ["executionDetails"] = new Dictionary<string, object>
            {
                ["eventName"] = "Completed",
                ["targetActivityId"] = "b961ee5b6f66b01c"
            },
            ["assertionContext"] = new Dictionary<string, object>
            {
                ["expectedCondition"] = false,
                ["testScenario"] = "false condition path"
            }
        };

        var templateInfo = new TemplateExecutionInfo
        {
            TemplateName = "execute-workflow-and-get-activity",
            StepsExecuted = 3,
            SavedVariables = new Dictionary<string, object>
            {
                ["ifConditionResult"] = ifConditionResult
            }
        };

        var stepInfo = new StepDebugInfo
        {
            TestNumber = 1,
            StepNumber = 1,
            StepType = "UseStep",
            StepId = "execute-workflow-test",
            Enabled = true,
            Result = "Success",
            Duration = TimeSpan.FromMilliseconds(750),
            TemplateExecution = templateInfo
        };

        // Create assertions with test scenario context
        var assertionResults = new List<AssertionResult>
        {
            new AssertionResult(true)
            {
                Operation = "equals",
                Description = "Activity condition should match expected value for false condition path",
                ActualValue = false,
                ExpectedValue = false
            },
            new AssertionResult(true)
            {
                Operation = "exists",
                Description = "Workflow instance ID should exist for false condition path",
                ActualValue = "b961ee5b6f66b01c"
            }
        };

        // Act
        logger.LogStepExecution(stepInfo);
        logger.LogAssertionResults(assertionResults);
        var output = logger.GetOutput();

        // Assert - Verify all improvements are present
        Console.WriteLine("=== IMPROVED TEMPLATE EXECUTION DEBUG OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");

        // 1. Detailed saved variables (not just "{object with X properties}")
        Assert.Contains("Saved variables:", output);
        Assert.Contains("ifConditionResult", output);
        Assert.Contains("View ifConditionResult details", output);
        Assert.Contains("activityExecution", output);
        Assert.Contains("workflowInstanceId", output);
        Assert.Contains("b961ee5b6f66b01c", output);

        // 2. Enhanced assertion reporting with test scenario context
        Assert.Contains("Activity condition should match expected value for false condition path : PASSED ✅", output);
        Assert.Contains("Workflow instance ID should exist for false condition path : PASSED ✅", output);
        Assert.Contains("false condition path", output);
        
        // 3. No JSONPath clutter
        Assert.DoesNotContain("For Assertions: You can now reference these JSONPath expressions:", output);
        
        // 4. Collapsible detailed content
        Assert.Contains("<details>", output);
        Assert.Contains("<summary>", output);
        Assert.Contains("```json", output);
    }

    [Fact]
    public void MarkdownDebugLogger_ShouldNotShowJSONPathClutter()
    {
        // Arrange
        var logger = new MarkdownDebugLogger();
        var changes = new ContextChanges();
        changes.Added.Add("`$.workflowResult` = {object with 5 properties}");
        changes.Available.Add("$.workflowResult");

        // Act
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();

        // Assert - Clutter message should be removed (improvement)
        Assert.DoesNotContain("For Assertions: You can now reference these JSONPath expressions:", output);
        Assert.Contains("**Context Changes:**", output); // But context changes still shown
    }

    [Fact]
    public void MarkdownDebugLogger_LogTestSummary_WithFailedAssertions_ShowsFailureDetails()
    {
        // Arrange
        var logger = new MarkdownDebugLogger();
        
        var testResults = new List<JTestCaseResult>
        {
            new JTestCaseResult
            {
                TestCaseName = "Authentication Test",
                Success = false,
                StepResults = new List<StepResult>
                {
                    new StepResult
                    {
                        Success = false,
                        AssertionResults = new List<AssertionResult>
                        {
                            new AssertionResult(false)
                            {
                                Operation = "equals",
                                Description = "Token should be valid",
                                ActualValue = "invalid",
                                ExpectedValue = "valid",
                                ErrorMessage = "Token validation failed"
                            }
                        }
                    }
                }
            },
            new JTestCaseResult
            {
                TestCaseName = "Data Processing Test",
                Success = true,
                StepResults = new List<StepResult>
                {
                    new StepResult
                    {
                        Success = true,
                        AssertionResults = new List<AssertionResult>
                        {
                            new AssertionResult(true)
                            {
                                Operation = "exists",
                                Description = "Data should exist"
                            }
                        }
                    }
                }
            }
        };

        // Act
        logger.LogTestSummary(testResults);
        var output = logger.GetOutput();

        // Assert
        Console.WriteLine("=== TEST SUMMARY OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");
        
        // Should contain summary statistics
        Assert.Contains("# Test Execution Summary", output);
        Assert.Contains("**Total Tests:** 2", output);
        Assert.Contains("**Successful:** 1 ✅", output);
        Assert.Contains("**Failed:** 1 ❌", output);
        
        // Should contain failed assertion details
        Assert.Contains("## Failed Assertions", output);
        Assert.Contains("**Test:** Authentication Test", output);
        Assert.Contains("**Assert:** Token should be valid : got `invalid` : expected `valid` ❌", output);
        Assert.Contains("**Error:** Token validation failed", output);
    }
    
    [Fact]
    public void MarkdownDebugLogger_LogTestSummary_WithAllPassingTests_ShowsSuccessMessage()
    {
        // Arrange
        var logger = new MarkdownDebugLogger();
        
        var testResults = new List<JTestCaseResult>
        {
            new JTestCaseResult
            {
                TestCaseName = "Test 1",
                Success = true,
                StepResults = new List<StepResult>
                {
                    new StepResult
                    {
                        Success = true,
                        AssertionResults = new List<AssertionResult>
                        {
                            new AssertionResult(true)
                            {
                                Operation = "equals",
                                Description = "Should pass"
                            }
                        }
                    }
                }
            }
        };

        // Act
        logger.LogTestSummary(testResults);
        var output = logger.GetOutput();

        // Assert
        Assert.Contains("🎉 **All tests passed successfully!**", output);
        Assert.Contains("**Total Tests:** 1", output);
        Assert.Contains("**Successful:** 1 ✅", output);
        Assert.Contains("**Failed:** 0 ❌", output);
    }
}