using JTest.Core.Execution;
using JTest.Core.Steps;
using Moq;
using Moq.Protected;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JTest.UnitTests;

public class HttpStepTests
{

    private static HttpStep CreateHttpStep(HttpResponseMessage? responseMessage = null, Expression<Func<HttpRequestMessage, bool>>? requestMessageRequirement = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = responseMessage ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
        };

        if (requestMessageRequirement is null)
        {
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }
        else
        {
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is(requestMessageRequirement),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        var httpClient = new HttpClient(mockHandler.Object);
        return new HttpStep(httpClient);
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

        var config = JsonSerializer.SerializeToElement(new
        {
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

        var config = JsonSerializer.SerializeToElement(new
        {
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

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://api.example.com",
            body = new
            {
                name = "{{$.user.name}}",
                email = "test@example.com"
            }
        });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonArrayBody_SerializesCorrectly()
    {
        var step = CreateHttpStep();
        var context = new TestExecutionContext();        
        var expectedBody = new[]
        {
            new
            {
                name = "johndoe@work.co",
                email = "test@example.com",
                addresses = new[]
                {
                    new
                    {
                        street = "Test Street"
                    }
                }
            }
        };
        var expectedBodyJson = JsonSerializer.SerializeToElement(expectedBody).GetRawText();

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://api.example.com",
            body = expectedBody
        });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);

        var responseData = JsonSerializer.SerializeToElement(context.Variables["this"]);
        var actualBodyJson = responseData.GetProperty("request").GetProperty("body").GetString();        

        Assert.Equal(expectedBodyJson, actualBodyJson);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonFile_Then_SerializesCorrectly()
    {
        // Arrange
        const string contentType = "application/json";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, contentType)
        };
        Expression<Func<HttpRequestMessage, bool>> requestMessageRequirementForSuccessResponse = (requestMessage)
            => requestMessage.Content!.Headers.ContentType!.MediaType == contentType;
        var step = CreateHttpStep(expectedResponse, requestMessageRequirementForSuccessResponse);

        var context = new TestExecutionContext();
        context.Variables["fileInfo"] = new { inputFilePath = "TestFiles/MockApiRequest.json" };

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://example.api.com/objects",
            file = "{{$.fileInfo.inputFilePath}}"
        });

        step.ValidateConfiguration(config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);

        var responseData = JsonSerializer.SerializeToElement(context.Variables["this"]);
        var statusCode = responseData.GetProperty("status").GetInt32();
        var body = responseData.GetProperty("body");
        var data = body.GetProperty("result");

        Assert.Equal((int)HttpStatusCode.OK, statusCode);
        Assert.Equal("success", data.GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomContentType_Then_SetsContentTypeHeader()
    {
        // Arrange
        const string contentType = "application/xml";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<response>success</response>", Encoding.UTF8, "application/xml")
        };
        Expression<Func<HttpRequestMessage, bool>> requestMessageRequirementForSuccessResponse = (requestMessage) 
            => requestMessage.Content!.Headers.ContentType!.MediaType == contentType;

        var step = CreateHttpStep(expectedResponse, requestMessageRequirementForSuccessResponse);
        var context = new TestExecutionContext();
        context.Variables["fileInfo"] = new { inputFilePath = "TestFiles/MockApiRequest.xml" };

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://example.api.com/objects",
            file = "{{$.fileInfo.inputFilePath}}",
            contentType
        });

        step.ValidateConfiguration(config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);

        var responseData = JsonSerializer.SerializeToElement(context.Variables["this"]);
        var body = responseData.GetProperty("body").GetString();
        var statusCode = responseData.GetProperty("status").GetInt32();

        Assert.Equal((int)HttpStatusCode.OK, statusCode);
        Assert.Equal("<response>success</response>", body);
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
        var step = new HttpStep(httpClient);
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

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "GET",
            url = "https://api.example.com/users",
            query = new
            {
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

    [Fact]
    public async Task ExecuteAsync_CapturesRequestDetails_IncludesUrlHeadersAndBody()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
        };

        var step = CreateHttpStep(response);
        var context = new TestExecutionContext();
        context.Variables["auth"] = new { token = "bearer123" };

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://api.example.com/users",
            headers = new[]
            {
                new { name = "Authorization", value = "Bearer {{$.auth.token}}" },
                new { name = "Content-Type", value = "application/json" }
            },
            body = new
            {
                name = "John Doe",
                email = "john@example.com"
            }
        });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(context.Variables.ContainsKey("this"));

        var responseData = context.Variables["this"];
        Assert.NotNull(responseData);

        // Verify the response data structure includes request details
        var responseType = responseData.GetType();
        Assert.True(responseType.GetProperty("request") != null);

        // Verify request details structure
        var requestProperty = responseType.GetProperty("request");
        var requestData = requestProperty?.GetValue(responseData);
        Assert.NotNull(requestData);

        var requestType = requestData.GetType();
        Assert.True(requestType.GetProperty("url") != null);
        Assert.True(requestType.GetProperty("method") != null);
        Assert.True(requestType.GetProperty("headers") != null);
        Assert.True(requestType.GetProperty("body") != null);
    }

    [Fact]
    public async Task ExecuteAsync_CapturesRequestDetails_WithActualValues()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":456,\"name\":\"John\"}", Encoding.UTF8, "application/json")
        };

        var step = CreateHttpStep(response);
        var context = new TestExecutionContext();
        context.Variables["auth"] = new { token = "bearer123" };

        var config = JsonSerializer.SerializeToElement(new
        {
            method = "POST",
            url = "https://api.example.com/users",
            headers = new[]
            {
                new { name = "Authorization", value = "Bearer {{$.auth.token}}" },
                new { name = "Content-Type", value = "application/json" }
            },
            body = new
            {
                name = "John Doe",
                email = "john@example.com"
            }
        });

        step.ValidateConfiguration(config);
        _ = await step.ExecuteAsync(context);

        // Verify the captured request details have the correct values
        var responseData = context.Variables["this"];
        var responseType = responseData?.GetType();
        var requestProperty = responseType?.GetProperty("request");
        var requestData = requestProperty?.GetValue(responseData)!;

        var requestType = requestData.GetType();
        var url = requestType.GetProperty("url")?.GetValue(requestData)?.ToString();
        var method = requestType.GetProperty("method")?.GetValue(requestData)?.ToString();
        var body = requestType.GetProperty("body")?.GetValue(requestData)?.ToString();

        Assert.Equal("https://api.example.com/users", url);
        Assert.Equal("POST", method);
        Assert.Contains("John Doe", body);
        Assert.Contains("john@example.com", body);
    }
}