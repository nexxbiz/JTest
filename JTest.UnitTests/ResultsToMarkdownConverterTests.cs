using JTest.Core.Converters;
using JTest.Core.Steps;
using JTest.Core.Models;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using System.Text.Json;
using Xunit;

namespace JTest.UnitTests;

public class ResultsToMarkdownConverterTests
{
    [Fact]
    public void ConvertToMarkdown_WithSavedValues_DisplaysSavedValuesSection()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockStep = new MockTestStep();
        
        var contextChanges = new ContextChanges();
        contextChanges.Added.Add("this", new Dictionary<string, object> { ["test"] = "value" });
        contextChanges.Added.Add("newVar", "test-value");
        contextChanges.Added.Add("$.globals.token", "abc123");
        contextChanges.Modified.Add("existingVar", "updated-value");

        var stepResult = new StepResult
        {
            Step = mockStep,
            Success = true,
            DurationMs = 150,
            ContextChanges = contextChanges
        };

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Test Case with Saves",
            Success = true,
            DurationMs = 200,
            StepResults = new List<StepResult> { stepResult }
        };

        var results = new List<JTestCaseResult> { testCaseResult };

        // Act
        var markdown = converter.ConvertToMarkdown(results);

        // Assert
        Assert.Contains("**Saved Values:**", markdown);
        Assert.Contains("**Added:** newVar = \"test-value\"", markdown);
        Assert.Contains("**Added:** $.globals.token = \"masked\"", markdown); // Sensitive values should be masked
        Assert.Contains("**Modified:** existingVar = \"updated-value\"", markdown);
        Assert.DoesNotContain("**Added:** this =", markdown); // Should be filtered out
        Assert.DoesNotContain("abc123", markdown); // Sensitive token value should not appear in output
    }

    [Fact]
    public void ConvertToMarkdown_WithNoSavedValues_DoesNotDisplaySavedValuesSection()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockStep = new MockTestStep();
        
        var stepResult = new StepResult
        {
            Step = mockStep,
            Success = true,
            DurationMs = 150,
            ContextChanges = null
        };

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Test Case without Saves",
            Success = true,
            DurationMs = 200,
            StepResults = new List<StepResult> { stepResult }
        };

        var results = new List<JTestCaseResult> { testCaseResult };

        // Act
        var markdown = converter.ConvertToMarkdown(results);

        // Assert
        Assert.DoesNotContain("**Saved Values:**", markdown);
    }

    [Fact]
    public void ConvertToMarkdown_WithInnerSteps_DisplaysTemplateSteps()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockTemplateStep = new MockTestStep { Type = "use" };
        var mockInnerStep1 = new MockTestStep { Type = "wait" };
        var mockInnerStep2 = new MockTestStep { Type = "http" };
        
        // Create inner step results
        var innerStep1Result = new StepResult
        {
            Step = mockInnerStep1,
            Success = true,
            DurationMs = 100,
            DetailedDescription = "Wait 100ms"
        };
        
        var innerStep2Result = new StepResult
        {
            Step = mockInnerStep2,
            Success = false,
            DurationMs = 50,
            ErrorMessage = "HTTP request failed",
            DetailedDescription = "HTTP GET request"
        };
        
        // Create main template step result and add inner results using reflection
        var templateStepResult = new StepResult
        {
            Step = mockTemplateStep,
            Success = true,
            DurationMs = 200
        };

        // Use reflection to set the internal property
        var innerResultsProperty = typeof(StepResult).GetProperty("InnerResults");
        var innerResultsList = (List<StepResult>)innerResultsProperty!.GetValue(templateStepResult)!;
        innerResultsList.Add(innerStep1Result);
        innerResultsList.Add(innerStep2Result);

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Test with Template Steps",
            Success = true,
            DurationMs = 250,
            StepResults = new List<StepResult> { templateStepResult }
        };

        var results = new List<JTestCaseResult> { testCaseResult };

        // Act
        var markdown = converter.ConvertToMarkdown(results);

        // Assert - Verify template steps section is shown
        Assert.Contains("**Template Steps:**", markdown);
        Assert.Contains("**Wait 100ms:** PASSED (100ms)", markdown);
        Assert.Contains("**HTTP GET request:** FAILED (50ms)", markdown);
        Assert.Contains("**Error:** HTTP request failed", markdown);
    }
}

/// <summary>
/// Mock step for testing purposes
/// </summary>
public class MockTestStep : IStep
{
    public string Type { get; set; } = "test";
    public string? Id { get; set; }

    public bool ValidateConfiguration(JsonElement configuration) => true;

    public Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        return Task.FromResult(StepResult.CreateSuccess(this));
    }

    public string GetStepDescription()
    {
        return "Mock test step";
    }
}