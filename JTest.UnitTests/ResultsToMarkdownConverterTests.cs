using JTest.Core.Converters;
using JTest.Core.Steps;
using JTest.Core.Models;
using JTest.Core.Debugging;
using Xunit;

namespace JTest.UnitTests;

public class ResultsToMarkdownConverterTests
{
    [Fact]
    public void ConvertToMarkdown_WithSavedValues_DisplaysSavedValuesSection()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        
        var contextChanges = new ContextChanges();
        contextChanges.Added.Add("this", new Dictionary<string, object> { ["test"] = "value" });
        contextChanges.Added.Add("newVar", "test-value");
        contextChanges.Added.Add("$.globals.token", "abc123");
        contextChanges.Modified.Add("existingVar", "updated-value");

        var stepResult = new StepResult
        {
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
        Assert.Contains("**Added:** $.globals.token = \"abc123\"", markdown);
        Assert.Contains("**Modified:** existingVar = \"updated-value\"", markdown);
        Assert.DoesNotContain("**Added:** this =", markdown); // Should be filtered out
    }

    [Fact]
    public void ConvertToMarkdown_WithNoSavedValues_DoesNotDisplaySavedValuesSection()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        
        var stepResult = new StepResult
        {
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
}