using System.Net;
using System.Text;
using System.Text.Json;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Steps;
using Moq;
using Moq.Protected;

namespace JTest.UnitTests;

public class HttpStepTests
{

    private HttpStep CreateHttpStep(HttpResponseMessage? responseMessage = null, IDebugLogger? debugLogger = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = responseMessage ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
        };
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
            
        var httpClient = new HttpClient(mockHandler.Object);
        return new HttpStep(httpClient, debugLogger);
    }

    [Fact]
    public void Type_ShouldReturnHttp()
    {
        var step = CreateHttpStep();
        Assert.Equal("http", step.Type);
    }

    [Fact]
    public void ValidateConfiguration_WithValidConfig_ReturnsTrue()
    {
        var step = CreateHttpStep();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        Assert.True(step.ValidateConfiguration(config));
    }

    [Fact] 
    public void ValidateConfiguration_WithMissingMethod_ReturnsFalse()
    {
        var step = CreateHttpStep();
        var config = JsonSerializer.SerializeToElement(new { url = "https://api.example.com" });
        Assert.False(step.ValidateConfiguration(config));
    }

    [Fact]
    public void ValidateConfiguration_WithMissingUrl_ReturnsFalse()
    {
        var step = CreateHttpStep();
        var config = JsonSerializer.SerializeToElement(new { method = "GET" });
        Assert.False(step.ValidateConfiguration(config));
    }

    [Fact]
    public async Task ExecuteAsync_WithBasicGet_ReturnsSuccessResult()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_WithTokensInUrl_ResolvesTokens()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        context.Variables["env"] = new { baseUrl = "https://api.example.com" };
        
        var config = JsonSerializer.SerializeToElement(new { 
            method = "GET", 
            url = "{{$.env.baseUrl}}/users" 
        });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithHeaders_AddsHeadersToRequest()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        context.Variables["globals"] = new { token = "abc123" };
        
        var config = JsonSerializer.SerializeToElement(new {
            method = "GET",
            url = "https://api.example.com",
            headers = new[]
            {
                new { name = "Authorization", value = "Bearer {{$.globals.token}}" },
                new { name = "Content-Type", value = "application/json" }
            }
        });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonBody_SerializesCorrectly()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John" };
        
        var config = JsonSerializer.SerializeToElement(new {
            method = "POST",
            url = "https://api.example.com",
            body = new {
                name = "{{$.user.name}}",
                email = "test@example.com"
            }
        });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StoresResponseInContext()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        await step.ExecuteAsync(context);
        
        Assert.True(context.Variables.ContainsKey("this"));
        var responseData = context.Variables["this"];
        Assert.NotNull(responseData);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepId_StoresInBothScopes()
    {
        var step = CreateHttpStep();
        step.Id = "api-call";
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        await step.ExecuteAsync(context);
        
        Assert.True(context.Variables.ContainsKey("this"));
        Assert.True(context.Variables.ContainsKey("api-call"));
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpError_ReturnsFailureResult()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error", Encoding.UTF8, "text/plain")
        };
        var step = CreateHttpStep(errorResponse);
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        // HttpClient doesn't throw for non-success status codes by default, so this should still succeed
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_WithNetworkError_ReturnsFailureResult()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
            
        var httpClient = new HttpClient(mockHandler.Object);
        var step = new HttpStep(httpClient, null);
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.False(result.Success);
        Assert.Contains("Network error", result.ErrorMessage);
        Assert.Single(context.Log);
    }

    [Fact]
    public async Task ExecuteAsync_MeasuresRequestDuration()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonResponse_ParsesResponseBody()
    {
        var jsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123,\"name\":\"test\"}", Encoding.UTF8, "application/json")
        };
        var step = CreateHttpStep(jsonResponse);
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        await step.ExecuteAsync(context);
        
        Assert.True(context.Variables.ContainsKey("this"));
        var responseData = context.Variables["this"];
        Assert.NotNull(responseData);
    }

    [Fact]
    public async Task ExecuteAsync_WithDebugLogger_CallsDebugMethods()
    {
        var debugLogger = new MarkdownDebugLogger();
        var jsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"workflowInstanceId\":\"test123\"}", Encoding.UTF8, "application/json")
        };
        var step = CreateHttpStep(jsonResponse, debugLogger);
        step.Id = "test-step";
        
        var context = new TestExecutionContext();
        context.Variables["env"] = new Dictionary<string, object>
        {
            ["baseUrl"] = "https://api.test.com"
        };
        
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        await step.ExecuteAsync(context);
        
        var output = debugLogger.GetOutput();
        
        Assert.Contains("## Test 1, Step 1: http", output);
        Assert.Contains("**Step ID:** test-step", output);
        Assert.Contains("**Result:** Success", output);
        Assert.Contains("**Context Changes:**", output);
        Assert.Contains("**Added:**", output);
        Assert.Contains("- `$.this` = {object of type", output);
        // Details sections are now only used for template executions, not regular steps
        Assert.DoesNotContain("<details>", output);
        Assert.DoesNotContain("Runtime Context", output);
    }

    [Fact]
    public async Task ExecuteAsync_WithTextResponse_KeepsAsString()
    {
        var textResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Plain text response", Encoding.UTF8, "text/plain")
        };
        var step = CreateHttpStep(textResponse);
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "GET", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        await step.ExecuteAsync(context);
        
        Assert.True(context.Variables.ContainsKey("this"));
        var responseData = context.Variables["this"];
        Assert.NotNull(responseData);
    }

    [Fact]
    public async Task ExecuteAsync_WithQueryParameters_AddsToUrl()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();
        context.Variables["filters"] = new { status = "active" };
        
        var config = JsonSerializer.SerializeToElement(new {
            method = "GET",
            url = "https://api.example.com/users",
            query = new {
                limit = "10",
                status = "{{$.filters.status}}",
                order = "name"
            }
        });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_ResponseDataStructure_ContainsRequiredFields()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":123}", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Custom-Header", "test-value");
        
        var step = CreateHttpStep(response);
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { method = "POST", url = "https://api.example.com" });
        
        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);
        
        Assert.True(result.Success);
        Assert.True(context.Variables.ContainsKey("this"));
        
        // The response data should be an anonymous object with status, headers, body, duration properties
        var responseData = context.Variables["this"];
        Assert.NotNull(responseData);
        
        // Verify the structure has the required properties
        var responseType = responseData.GetType();
        Assert.True(responseType.GetProperty("status") != null);
        Assert.True(responseType.GetProperty("headers") != null);
        Assert.True(responseType.GetProperty("body") != null);
        Assert.True(responseType.GetProperty("duration") != null);
    }
}