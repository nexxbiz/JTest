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
        
        Assert.Contains("**Result:** ✅ Success", output);
        Assert.Contains("**Duration:** 332,74ms", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_WithNoChanges_ShowsNone()
    {
        var logger = new MarkdownDebugLogger();
        var changes = new ContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("📋 **Context Changes:** None", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_WithChanges_ShowsAddedAndModified()
    {
        var logger = new MarkdownDebugLogger();
        var changes = CreateTestContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("**✅ Added:**", output);
        Assert.Contains("- `$.execute-workflow` = {object with 5 properties}", output);
        Assert.Contains("- `$.workflowInstanceId` = \"b92e57abae5e5873\"", output);
        Assert.Contains("**🔄 Modified:**", output);
        Assert.Contains("- `$.this`: {object with 0 properties} → {object with 3 properties}", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogContextChanges_GeneratesAssertionGuidance()
    {
        var logger = new MarkdownDebugLogger();
        var changes = CreateTestContextChanges();
        
        logger.LogContextChanges(changes);
        var output = logger.GetOutput();
        
        Assert.Contains("💡 **For Assertions:** You can now reference these JSONPath expressions:", output);
        Assert.Contains("- `$.execute-workflow` or `{{ $.execute-workflow }}`", output);
        Assert.Contains("  - Example: `$.execute-workflow.status`", output);
        Assert.Contains("- `$.workflowInstanceId` or `{{ $.workflowInstanceId }}`", output);
        Assert.Contains("- `$.this` or `{{ $.this }}`", output);
    }

    [Fact]
    public void MarkdownDebugLogger_LogRuntimeContext_GeneratesCollapsibleSection()
    {
        var logger = new MarkdownDebugLogger();
        var context = CreateTestRuntimeContext();
        
        logger.LogRuntimeContext(context);
        var output = logger.GetOutput();
        
        Assert.Contains("<details>", output);
        Assert.Contains("<summary>📋 Runtime Context (Click to expand)</summary>", output);
        Assert.Contains("```json", output);
        Assert.Contains("\"env\":", output);
        Assert.Contains("\"this\":", output);
        Assert.Contains("```", output);
        Assert.Contains("</details>", output);
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
            Result = "✅ Success"
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
        
        Assert.Contains("📋 **Context Changes:** None", output);
        Assert.DoesNotContain("💡 **For Assertions:**", output);
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
        
        // Verify complete output structure matches sample
        Assert.Contains("## Test 1, Step 1: HttpStep", output);
        Assert.Contains("**Step ID:** execute-workflow", output);
        Assert.Contains("**Duration:** 332,74ms", output);
        Assert.Contains("**✅ Added:**", output);
        Assert.Contains("**🔄 Modified:**", output);
        Assert.Contains("💡 **For Assertions:**", output);
        Assert.Contains("<details>", output);
        Assert.Contains("📋 Runtime Context", output);
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
            Result = "✅ Success",
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