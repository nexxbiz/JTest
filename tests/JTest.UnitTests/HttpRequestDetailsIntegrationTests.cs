using JTest.Core.Converters;
using JTest.Core.Steps;
using JTest.Core.Models;
using JTest.Core.Execution;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace JTest.UnitTests;

/// <summary>
/// Integration tests demonstrating end-to-end HTTP request details capture and display
/// </summary>
public class HttpRequestDetailsIntegrationTests
{
    [Fact]
    public async Task EndToEnd_HttpStepWithMarkdownConverter_DisplaysRequestDetails()
    {
        // Arrange: Create HTTP step with mock response
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":789,\"status\":\"created\"}", Encoding.UTF8, "application/json")
        };
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object);
        
        
        // Configure HTTP step with headers and body
        var context = new TestExecutionContext();
        context.Variables["auth"] = new { token = "secret123" };
        context.Variables["user"] = new { name = "Alice", email = "alice@example.com" };

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://api.example.com/users",
            headers = new[]
            {
                new { name = "Authorization", value = "Bearer {{$.auth.token}}" },
                new { name = "Content-Type", value = "application/json" },
                new { name = "X-Request-ID", value = "test-12345" }
            },
            body = new
            {
                name = "{{$.user.name}}",
                email = "{{$.user.email}}",
                role = "admin"
            }
        });

        var httpStep = new HttpStep(httpClient, config);

        // Act: Execute HTTP step
        var stepResult = await httpStep.ExecuteAsync(context);

        // Create test case result for markdown conversion
        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Create User with Request Details Demo",
            DurationMs = stepResult.DurationMs + 10,
            StepResults = new List<StepResult> { stepResult }
        };
        if(!stepResult.Success)
            testCaseResult.AddError(stepResult.ErrorMessage);

        // Convert to markdown
        var converter = new ResultsToMarkdownConverter();
        var markdown = converter.ConvertToMarkdown(new List<JTestCaseResult> { testCaseResult });

        // Assert: Verify request details are captured and displayed
        Assert.True(stepResult.Success);
        Assert.Contains("**HTTP Request:**", markdown);
        
        // Verify table structure
        Assert.Contains("<table>", markdown);
        Assert.Contains("<tr><th>Field</th><th>Value</th></tr>", markdown);
        
        // Verify URL is displayed
        Assert.Contains("<tr><td>URL</td><td>https://api.example.com/users</td></tr>", markdown);
        
        // Verify method is displayed
        Assert.Contains("<tr><td>Method</td><td>POST</td></tr>", markdown);
        
        // Verify headers are displayed (with security masking)
        Assert.Contains("<tr><td>Headers</td>", markdown);
        Assert.Contains("Authorization: masked", markdown); // Should be masked
        Assert.Contains("Content-Type: application/json", markdown);
        Assert.Contains("X-Request-ID: test-12345", markdown);
        
        // Verify sensitive token is masked
        Assert.DoesNotContain("secret123", markdown);
        
        // Verify body is displayed with collapsible JSON
        Assert.Contains("<tr><td>Body</td>", markdown);
        Assert.Contains("show JSON", markdown);
        Assert.Contains("Alice", markdown); // Resolved variable
        Assert.Contains("alice@example.com", markdown); // Resolved variable
        Assert.Contains("admin", markdown);
    }

    [Fact]
    public void EndToEnd_NonHttpStep_DoesNotDisplayRequestDetails()
    {
        // Arrange: Create a non-HTTP step
        var mockStep = new MockTestStep { Type = "wait" };
        
        var stepResult = new StepResult(1)
        {
            Step = mockStep,
            Success = true,
            DurationMs = 100,
            Data = new { waitTime = 100 }
        };

        var testCaseResult = new JTestCaseResult
        {
            TestCaseName = "Non-HTTP Step Test",            
            DurationMs = 110,
            StepResults = new List<StepResult> { stepResult }
        };

        // Act: Convert to markdown
        var converter = new ResultsToMarkdownConverter();
        var markdown = converter.ConvertToMarkdown(new List<JTestCaseResult> { testCaseResult });

        // Assert: Verify HTTP request details are NOT displayed for non-HTTP steps
        Assert.DoesNotContain("**HTTP Request:**", markdown);
        Assert.DoesNotContain("<tr><td>URL</td>", markdown);
        Assert.DoesNotContain("<tr><td>Method</td>", markdown);
    }

    private class MockTestStep : IStep
    {
        public string Type { get; set; } = "test";
        public string? Id { get; set; }


        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool ValidateConfiguration(List<string> validationErrors) => true;
        
        public Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StepResult.CreateSuccess(0, this));
        }        
    }
}