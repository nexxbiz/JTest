using JTest.Core.Assertions;
using JTest.Core.Debugging;

namespace JTest.UnitTests;

public class DebugLoggerTests
{
    [Fact]
    public void MarkdownDebugLogger_LogStepExecution_GeneratesCorrectHeader()
    {
        var logger = new MarkdownDebugLogger();
        var stepInfo = CreateTestStepInfo();
        
        logger.LogStepExecution(stepInfo);
        var output = logger.GetOutput();
        
        Assert.Contains("## Test 1, Step 1: HttpStep", output);
        Assert.Contains("**Step ID:** execute-workflow", output);
        Assert.Contains("**Step Type:** HttpStep", output);
        Assert.Contains("**Enabled:** True", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogStepExecution_FormatsResultAndDuration()
    {
        var logger = new MarkdownDebugLogger();
        var stepInfo = CreateTestStepInfo();
        
        logger.LogStepExecution(stepInfo);
        var output = logger.GetOutput();
        
        Assert.Contains("**Result:** Success", output);
        Assert.Contains("**Duration:** 332,74ms", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_WithNoChanges_ShowsNone()
    {
        var logger = new MarkdownDebugLogger();
        var changes = new ContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("**Context Changes:** None", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_WithChanges_ShowsAddedAndModified()
    {
        var logger = new MarkdownDebugLogger();
        var changes = CreateTestContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("**Added:**", output);
        Assert.Contains("- `$.execute-workflow` = {object with 5 properties}", output);
        Assert.Contains("- `$.workflowInstanceId` = \"b92e57abae5e5873\"", output);
        Assert.Contains("**Modified:**", output);
        Assert.Contains("- `$.this`: {object with 0 properties} → {object with 3 properties}", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_DoesNotGenerateAssertionGuidanceClutter()
    {
        var logger = new MarkdownDebugLogger();
        var changes = CreateTestContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        // Verify that the clutter is removed (improvement)
        Assert.DoesNotContain("**For Assertions:** You can now reference these JSONPath expressions:", output);
        
        // But context changes are still properly reported
        Assert.Contains("**Context Changes:**", output);
        Assert.Contains("**Added:**", output);
        Assert.Contains("**Modified:**", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogRuntimeContext_GeneratesCollapsibleSection()
    {
        var logger = new MarkdownDebugLogger();
        var context = CreateTestRuntimeContext();
        
        logger.LogRuntimeContext(context);
        var output = logger.GetOutput();
        
        // Since runtime context is no longer displayed per requirements
        // ("don't show the runtime context... any more anywhere"), 
        // this test should expect empty output
        Assert.Equal("", output.Trim());
    }

    [Fact]
    public void MarkdownDebugLogger_FormatDuration_UsesCommaDecimalSeparator()
    {
        var logger = new MarkdownDebugLogger();
        var stepInfo = new StepDebugInfo
        {
            Duration = TimeSpan.FromMilliseconds(332.74),
            TestNumber = 1,
            StepNumber = 1,
            StepType = "HttpStep",
            Result = "Success"
        };
        
        logger.LogStepExecution(stepInfo);
        var output = logger.GetOutput();
        
        Assert.Contains("332,74ms", output);
    }

    [Fact]
    public void MarkdownDebugLogger_ContextChanges_EmptyLists_ShowsNone()
    {
        var logger = new MarkdownDebugLogger();
        var changes = new ContextChanges
        {
            Added = new List<string>(),
            Modified = new List<string>(),
            Available = new List<string>()
        };
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("**Context Changes:** None", output);
        Assert.DoesNotContain("**For Assertions:**", output);
    }

    [Fact]
    public void MarkdownDebugLogger_FullWorkflow_GeneratesCompleteOutput()
    {
        var logger = new MarkdownDebugLogger();
        var stepInfo = CreateTestStepInfo();
        var changes = CreateTestContextChanges();
        var context = CreateTestRuntimeContext();
        
        logger.LogStepExecution(stepInfo);
        logger.LogContextChanges(changes);
        logger.LogRuntimeContext(context);
        
        var output = logger.GetOutput();
        
        // Verify complete output structure matches improved sample
        Assert.Contains("## Test 1, Step 1: HttpStep", output);
        Assert.Contains("**Step ID:** execute-workflow", output);
        Assert.Contains("**Duration:** 332,74ms", output);
        Assert.Contains("**Added:**", output);
        Assert.Contains("**Modified:**", output);
        Assert.DoesNotContain("**For Assertions:**", output); // Improved: clutter removed
        // Runtime context no longer displayed per requirements
        Assert.DoesNotContain("<details>", output);
        Assert.DoesNotContain("Runtime Context", output);
    }

    [Fact]
    public void MarkdownDebugLogger_EdgeCases_HandlesGracefully()
    {
        var logger = new MarkdownDebugLogger();
        
        // Test with empty step ID
        var stepInfo = new StepDebugInfo
        {
            TestNumber = 2,
            StepNumber = 3,
            StepType = "UseStep",
            StepId = "",
            Enabled = false,
            Result = "Failed",
            Duration = TimeSpan.FromMilliseconds(0.5),
            Description = ""
        };
        
        // Test with empty context changes
        var changes = new ContextChanges();
        
        // Test with minimal context
        var context = new Dictionary<string, object>
        {
            ["env"] = new Dictionary<string, object>(),
            ["this"] = new { status = 404 }
        };
        
        logger.LogStepExecution(stepInfo);
        logger.LogContextChanges(changes);
        logger.LogRuntimeContext(context);
        
        var output = logger.GetOutput();
        
        Assert.Contains("## Test 2, Step 3: UseStep", output);
        Assert.DoesNotContain("**Step ID:**", output); // Empty ID should not appear
        Assert.Contains("**Enabled:** False", output);
        Assert.Contains("**Result:** Failed", output);
        Assert.Contains("0,50ms", output); // Test decimal formatting
        Assert.Contains("**Context Changes:** None", output);
        Assert.DoesNotContain("**For Assertions:**", output); // No assertions for empty changes
    }

    [Fact]
    public void MarkdownDebugLogger_LogAssertionResults_WithSuccessfulAssertions_GeneratesCorrectOutput()
    {
        var logger = new MarkdownDebugLogger();
        var assertionResults = new List<AssertionResult>
        {
            new AssertionResult(true)
            {
                Operation = "equals",
                Description = "Check status code",
                ActualValue = 200,
                ExpectedValue = 200
            },
            new AssertionResult(true)
            {
                Operation = "exists",
                Description = "Check response body exists",
                ActualValue = "response data"
            }
        };
        
        logger.LogAssertionResults(assertionResults);
        var output = logger.GetOutput();
        
        // Updated to match new improved assertion format
        Assert.Contains("**Assertions:**", output);
        Assert.Contains("- Check status code : PASSED ✅", output);
        Assert.Contains("- Check response body exists : PASSED ✅", output);
        // New format doesn't show details for successful assertions
    }

    [Fact]
    public void MarkdownDebugLogger_LogAssertionResults_WithFailedAssertions_GeneratesCorrectOutput()
    {
        var logger = new MarkdownDebugLogger();
        var assertionResults = new List<AssertionResult>
        {
            new AssertionResult(false, "Expected 200 but got 404")
            {
                Operation = "equals",
                Description = "Check status code",
                ActualValue = 404,
                ExpectedValue = 200
            },
            new AssertionResult(false, "Value is null")
            {
                Operation = "exists",
                Description = "Check required field",
                ActualValue = null
            }
        };
        
        logger.LogAssertionResults(assertionResults);
        var output = logger.GetOutput();
        
        // Updated to match new improved assertion format
        Assert.Contains("**Assertions:**", output);
        Assert.Contains("- Check status code : got `404` : expected `200` : FAILED ❌", output);
        Assert.Contains("- Error: Expected 200 but got 404", output);
        Assert.Contains("- Check required field : got `null` : FAILED ❌", output);
        Assert.Contains("- Error: Value is null", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogAssertionResults_WithEmptyList_DoesNotAddSection()
    {
        var logger = new MarkdownDebugLogger();
        var assertionResults = new List<AssertionResult>();
        
        logger.LogAssertionResults(assertionResults);
        var output = logger.GetOutput();
        
        Assert.DoesNotContain("**Assertion Results:**", output);
        Assert.Equal("", output); // Should produce no output
    }

    [Fact]
    public void MarkdownDebugLogger_FullWorkflowWithAssertions_GeneratesCompleteOutput()
    {
        var logger = new MarkdownDebugLogger();
        var stepInfo = CreateTestStepInfo();
        var changes = CreateTestContextChanges();
        var context = CreateTestRuntimeContext();
        var assertionResults = new List<AssertionResult>
        {
            new AssertionResult(true)
            {
                Operation = "equals",
                ActualValue = 200,
                ExpectedValue = 200
            }
        };
        
        logger.LogStepExecution(stepInfo);
        logger.LogContextChanges(changes);
        logger.LogAssertionResults(assertionResults);
        logger.LogRuntimeContext(context);
        
        var output = logger.GetOutput();
        
        // Verify complete output structure includes assertion results  
        Assert.Contains("## Test 1, Step 1: HttpStep", output);
        Assert.Contains("**Duration:** 332,74ms", output);
        Assert.Contains("**Added:**", output);
        Assert.DoesNotContain("**For Assertions:**", output); // Improved: clutter removed
        // Updated to match new improved assertion format  
        Assert.Contains("**Assertions:**", output);
        Assert.Contains("-  : PASSED ✅", output); // No description or operation set in test data
        // Runtime context no longer displayed per requirements
        Assert.DoesNotContain("<details>", output);
        Assert.DoesNotContain("Runtime Context", output);
        
        // Print the complete debug output for demonstration
        Console.WriteLine("=== COMPLETE DEBUG MARKDOWN OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");
    }

    private StepDebugInfo CreateTestStepInfo()
    {
        return new StepDebugInfo
        {
            TestNumber = 1,
            StepNumber = 1,
            StepType = "HttpStep",
            StepId = "execute-workflow",
            Enabled = true,
            Result = "Success",
            Duration = TimeSpan.FromMilliseconds(332.74),
            Description = "Execute workflow HTTP request"
        };
    }

    private ContextChanges CreateTestContextChanges()
    {
        return new ContextChanges
        {
            Added = new List<string>
            {
                "`$.execute-workflow` = {object with 5 properties}",
                "`$.workflowInstanceId` = \"b92e57abae5e5873\""
            },
            Modified = new List<string>
            {
                "`$.this`: {object with 0 properties} → {object with 3 properties}"
            },
            Available = new List<string>
            {
                "$.execute-workflow",
                "$.workflowInstanceId", 
                "$.this"
            }
        };
    }

    private Dictionary<string, object> CreateTestRuntimeContext()
    {
        return new Dictionary<string, object>
        {
            ["env"] = new Dictionary<string, object>
            {
                ["baseUrl"] = "https://api.totest.com",
                ["username"] = "myUsername",
                ["password"] = "myPassword"
            },
            ["globals"] = new Dictionary<string, object>(),
            ["case"] = new Dictionary<string, object>(),
            ["ctx"] = new Dictionary<string, object>(),
            ["this"] = new Dictionary<string, object>
            {
                ["status"] = 200,
                ["body"] = new Dictionary<string, object>
                {
                    ["workflowInstanceId"] = "b92e57abae5e5873"
                }
            }
        };
    }
}