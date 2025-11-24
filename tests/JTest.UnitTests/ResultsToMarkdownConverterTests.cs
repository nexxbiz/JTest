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

        var stepResult = new StepResult(1)
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
        Assert.Contains("<table>", markdown);
        Assert.Contains("<tr><td>Added</td><td>newVar</td><td>\"test-value\"</td></tr>", markdown);
        Assert.Contains("<tr><td>Added</td><td>$.globals.token</td><td>\"masked\"</td></tr>", markdown); // Sensitive values should be masked
        Assert.Contains("<tr><td>Modified</td><td>existingVar</td><td>\"updated-value\"</td></tr>", markdown);
        Assert.DoesNotContain("<tr><td>Added</td><td>this</td>", markdown); // Should be filtered out
        Assert.DoesNotContain("abc123", markdown); // Sensitive token value should not appear in output
    }

    [Fact]
    public void ConvertToMarkdown_WithNoSavedValues_DoesNotDisplaySavedValuesSection()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockStep = new MockTestStep();
        
        var stepResult = new StepResult(1)
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
        var innerStep1Result = new StepResult(0)
        {
            Step = mockInnerStep1,
            Success = true,
            DurationMs = 100,
            DetailedDescription = "Wait 100ms"
        };
        
        var innerStep2Result = new StepResult(1)
        {
            Step = mockInnerStep2,
            Success = false,
            DurationMs = 50,
            ErrorMessage = "HTTP request failed",
            DetailedDescription = "HTTP GET request"
        };
        
        // Create main template step result and add inner results using reflection
        var templateStepResult = new StepResult(2)
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

        // Assert - Verify template steps section is shown in collapsible format
        Assert.Contains("<details>", markdown);
        Assert.Contains("<summary><strong>Template Steps</strong></summary>", markdown);
        Assert.Contains("<table>", markdown);
        Assert.Contains("<tr><td>Wait 100ms</td><td>PASSED</td><td>100ms</td><td></td></tr>", markdown);
        Assert.Contains("<tr><td>HTTP GET request</td><td>FAILED</td><td>50ms</td><td>Error: HTTP request failed</td></tr>", markdown);
        Assert.Contains("</details>", markdown);
    }

    [Fact]
    public void ConvertToMarkdown_WithAssertions_DisplaysAssertionsAsTable()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockStep = new MockTestStep();
        
        var assertion1 = new JTest.Core.Assertions.AssertionResult(true) { Description = "Workflow instance ID should exist", ActualValue = "7b05846c2a980394" };
        var assertion2 = new JTest.Core.Assertions.AssertionResult(true) { Description = "Workflow expected end value: 10", ActualValue = "10", ExpectedValue = "10" };
        var assertion3 = new JTest.Core.Assertions.AssertionResult(false) { Description = "Activity execution numbers expected : 11", ActualValue = "9", ExpectedValue = "11" };
        
        var stepResult = new StepResult(0)
        {
            Step = mockStep,
            Success = true,
            DurationMs = 150,
            AssertionResults = new List<JTest.Core.Assertions.AssertionResult> { assertion1, assertion2, assertion3 }
        };

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Test Case with Assertions",
            Success = true,
            DurationMs = 200,
            StepResults = new List<StepResult> { stepResult }
        };

        var results = new List<JTestCaseResult> { testCaseResult };

        // Act
        var markdown = converter.ConvertToMarkdown(results);

        // Assert - Verify assertions section uses HTML table
        Assert.Contains("**Assertions:**", markdown);
        Assert.Contains("<table>", markdown);
        Assert.Contains("<thead>", markdown);
        Assert.Contains("<tr><th>Status</th><th>Description</th><th>Actual</th><th>Expected</th></tr>", markdown);
        Assert.Contains("<tbody>", markdown);
        Assert.Contains("<tr><td>PASSED</td><td>Workflow instance ID should exist</td><td>7b05846c2a980394</td><td></td></tr>", markdown);
        Assert.Contains("<tr><td>PASSED</td><td>Workflow expected end value: 10</td><td>10</td><td>10</td></tr>", markdown);
        Assert.Contains("<tr><td>FAILED</td><td>Activity execution numbers expected : 11</td><td>9</td><td>11</td></tr>", markdown);
    }

    [Fact]
    public void ConvertToMarkdown_WithHttpStepRequestDetails_DisplaysHttpRequestTable()
    {
        // Arrange
        var converter = new ResultsToMarkdownConverter();
        var mockHttpStep = new MockTestStep { Type = "http" };

        // Create mock request data structure that matches what HttpStep produces
        var requestData = new
        {
            url = "https://api.example.com/users",
            method = "POST",
            headers = new[]
            {
                new { name = "Authorization", value = "Bearer token123" },
                new { name = "Content-Type", value = "application/json" }
            },
            body = "{\"name\":\"John Doe\",\"email\":\"john@example.com\"}"
        };

        var responseData = new
        {
            status = 201,
            headers = new[] { new { name = "Content-Type", value = "application/json" } },
            body = new { id = 123, name = "John Doe" },
            duration = 250,
            request = requestData
        };

        var stepResult = new StepResult(0)
        {
            Step = mockHttpStep,
            Success = true,
            DurationMs = 250,
            Data = responseData
        };

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Test HTTP Request Display",
            Success = true,
            DurationMs = 300,
            StepResults = new List<StepResult> { stepResult }
        };

        var results = new List<JTestCaseResult> { testCaseResult };

        // Act
        var markdown = converter.ConvertToMarkdown(results);

        // Assert
        Assert.Contains("**HTTP Request:**", markdown);
        Assert.Contains("<table>", markdown);
        Assert.Contains("<tr><th>Field</th><th>Value</th></tr>", markdown);
        Assert.Contains("<tr><td>URL</td><td>https://api.example.com/users</td></tr>", markdown);
        Assert.Contains("<tr><td>Method</td><td>POST</td></tr>", markdown);
        Assert.Contains("<tr><td>Headers</td>", markdown);
        Assert.Contains("Authorization: masked", markdown); // Should be masked due to sensitive header
        Assert.Contains("Content-Type: application/json", markdown);
        Assert.Contains("<tr><td>Body</td>", markdown);
        Assert.Contains("show JSON", markdown); // Should be collapsible JSON
        
        // Sensitive token should be masked
        Assert.DoesNotContain("token123", markdown);
    }
}

/// <summary>
/// Mock step for testing purposes
/// </summary>
public class MockTestStep : IStep
{
    public string Type { get; set; } = "test";
    public string? Id { get; }

    public string? Name { get; }

    public string? Description { get; }

    public bool ValidateConfiguration(List<string> validationErrors) => true;

    public Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StepResult.CreateSuccess(0, this));
    }
}