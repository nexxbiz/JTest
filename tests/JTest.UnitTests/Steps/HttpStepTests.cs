using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using Moq;
using Moq.Protected;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JTest.UnitTests.Steps;

public sealed class HttpStepTests
{
    [Fact]
    public void Type_ShouldReturnHttp()
    {
        var step = CreateHttpStep();
        Assert.Equal("http", step.TypeName);
    }

    [Fact]
    public void When_Validate_WithValidConfig_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var step = CreateHttpStep(configuration: new("GET", "https://api.example.com"));

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.True(result);
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(InvalidConfigTestMemberData))]
    public void When_Validate_WithInvalidConfig_ReturnsFalse(HttpStepConfiguration invalidConfig, string expectedValidationErrorContains)
    {
        // Arrange
        var context = new TestExecutionContext();
        var step = CreateHttpStep(configuration: invalidConfig);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, err => err.Contains(expectedValidationErrorContains));
    }

    [Fact]
    public async Task ExecuteAsync_WithBasicGet_ReturnsSuccessResult()
    {
        // Arrange
        var context = new TestExecutionContext();
        var step = CreateHttpStep(configuration: new("GET", "https://api.example.com"));

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        var resultData = result.Data;
        Assert.NotNull(resultData);
        Assert.Equal("200", $"{resultData["status"]}");
    }

    [Fact]
    public async Task ExecuteAsync_WithTokensInUrl_ResolvesTokens()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["env"] = new { baseUrl = "https://api.example.com" };

        var step = CreateHttpStep(configuration: new("GET", "{{$.env.baseUrl}}/users"));

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        var resultData = result.Data;
        Assert.NotNull(resultData);
        Assert.Equal("200", $"{resultData["status"]}");
    }

    [Fact]
    public async Task ExecuteAsync_WithHeaders_AddsHeadersToRequest()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["globals"] = new { token = "abc123" };

        var step = CreateHttpStep(
            requestMessageRequirement: (msg) => msg.Headers.Authorization!.Parameter == "abc123",
            configuration: new("GET", "https://api.example.com", Headers: [new("Authorization", "Bearer {{$.globals.token}}")])
        );

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("200", $"{result.Data["status"]}");
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonBody_SerializesCorrectly()
    {
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John Doe User" };

        var step = CreateHttpStep(
            configuration: new("GET", "https://api.example.com", Body: new { name = "{{$.user.name}}", email = "test@example.com" })
        );

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("200", $"{result.Data["status"]}");
        var request = result.Data["request"] as Dictionary<string, object?>;
        Assert.NotNull(request);
        Assert.Contains("John Doe User", $"{request["body"]}");
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

        var context = new TestExecutionContext();
        context.Variables["fileInfo"] = new { inputFilePath = "TestFiles/MockApiRequest.json" };

        var configuration = new HttpStepConfiguration("POST", "https://example.api.com/objects", File: "{{$.fileInfo.inputFilePath}}");
        var step = CreateHttpStep(expectedResponse, requestMessageRequirementForSuccessResponse, configuration);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);

        var responseData = JsonSerializer.SerializeToElement(result.Data);
        var statusCode = responseData.GetProperty("status").GetInt32();
        var body = responseData.GetProperty("body");
        var data = body.GetProperty("result");

        Assert.Equal((int)HttpStatusCode.OK, statusCode);
        Assert.Equal("success", data.GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithFormFiles_Then_SendsMultiPartFormDataContent()
    {
        // Arrange        
        const int expectedMultiPartFormDataCount = 2;
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
        };
        Expression<Func<HttpRequestMessage, bool>> requestMessageRequirementForSuccessResponse = (requestMessage)
            => requestMessage.Content!.GetType() == typeof(MultipartFormDataContent)
                && ((MultipartFormDataContent)requestMessage.Content!).Count() == expectedMultiPartFormDataCount;

        var context = new TestExecutionContext();

        var formFiles = new HttpStepFormFileConfiguration[]
        {
            new("testFileText","mock-request.txt","TestFiles/MockApiRequest.txt", "plain/text"),
            new("testFileZip","mock-request.zip","TestFiles/MockApiRequest.zip", "application/octet-stream")
        };
        var configuration = new HttpStepConfiguration("POST", "https://api.example.com/files", FormFiles: formFiles);
        var step = CreateHttpStep(expectedResponse, requestMessageRequirementForSuccessResponse, configuration);


        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);

        var responseData = JsonSerializer.SerializeToElement(result.Data);
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

        var context = new TestExecutionContext();
        context.Variables["fileInfo"] = new { inputFilePath = "TestFiles/MockApiRequest.xml" };

        var config = new HttpStepConfiguration(
            Method: "POST",
            Url: "https://example.api.com/objects",
            File: "{{$.fileInfo.inputFilePath}}",
            ContentType: contentType
        );

        var step = CreateHttpStep(expectedResponse, requestMessageRequirementForSuccessResponse, config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);

        var responseData = JsonSerializer.SerializeToElement(result.Data);
        var body = responseData.GetProperty("body").GetString();
        var statusCode = responseData.GetProperty("status").GetInt32();

        Assert.Equal((int)HttpStatusCode.OK, statusCode);
        Assert.Equal("<response>success</response>", body);
    }

    [Fact]
    public async Task ExecuteAsync_WithTextResponse_KeepsAsString()
    {
        // Arrange
        var textResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Plain text response", Encoding.UTF8, "text/plain")
        };
        var context = new TestExecutionContext();
        var config = new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com");
        var step = CreateHttpStep(textResponse, null, config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        var responseData = JsonSerializer.SerializeToElement(result.Data);
        var body = responseData.GetProperty("body").GetString();
        Assert.Equal("Plain text response", body);
    }

    [Fact]
    public async Task ExecuteAsync_WithQueryParameters_AddsToUrl()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["filters"] = new { status = "active" };        
        var config = new HttpStepConfiguration(

            Method: "GET",
            Url: "https://api.example.com/users",
            Query: new Dictionary<string, string>
            {
                ["limit"] = "10",
                ["status"] = "{{$.filters.status}}"
            }
        );

        var step = CreateHttpStep(
            requestMessageRequirement: msg => msg.RequestUri!.Query.Contains("status=active") && msg.RequestUri!.Query.Contains("limit=10"),
            configuration: config
        );
        
        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);
        var responseData = JsonSerializer.SerializeToElement(result.Data);
        var statusCode = responseData.GetProperty("status").GetInt32();

        Assert.Equal((int)HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task ExecuteAsync_ResponseDataStructure_ContainsRequiredFields()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":123}", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Custom-Header", "test-value");

        var context = new TestExecutionContext();
        var config = new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com");
        var step = CreateHttpStep(response, null, config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert        
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.True(result.Data.ContainsKey("headers"));
        Assert.True(result.Data.ContainsKey("body"));
        Assert.True(result.Data.ContainsKey("request"));

        var request = result.Data["request"] as Dictionary<string, object?>;
        Assert.NotNull(request);
        Assert.True(request.ContainsKey("method"));
        Assert.True(request.ContainsKey("headers"));
        Assert.True(request.ContainsKey("body"));
        Assert.True(request.ContainsKey("url"));
    }

    public static readonly IEnumerable<object[]> InvalidConfigTestMemberData =
    [
        [new HttpStepConfiguration(Method: null!, "https://api.example.com"), "Invalid HTTP Method"],
        [new HttpStepConfiguration(Method: string.Empty, "https://api.example.com"), "Invalid HTTP Method"],
        [new HttpStepConfiguration(Method: "UNKNOWN METHOD", "https://api.example.com"), "Invalid HTTP Method"],
        [new HttpStepConfiguration(Method: "GET", Url: null!), "Invalid url"],
        [new HttpStepConfiguration(Method: "GET", Url: string.Empty), "Invalid url"],
        [new HttpStepConfiguration(Method: "GET", Url: "invalid uri"), "Invalid url"],

        [new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com", Body: new { prop="value" }, File: "path.json"), "You can only specify 1 body type"],
        [new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com", Body: new { prop="value" }, FormFiles: [new("file", "fileName", "path.json", "application/json")]), "You can only specify 1 body type"],
        [new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com", Body: null, File: "path.json", FormFiles: [new("file", "fileName", "path.json", "application/json")]), "You can only specify 1 body type"],

        [new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com", Body: null, File: "unknown-file.json"), "No file found at path"],
        [new HttpStepConfiguration(Method: "GET", Url: "https://api.example.com", Body: null, File: null, FormFiles: [new("file", "fileName", "unknown-file.json", "application/json")]), "No file found at path"]
    ];

    private static HttpStep CreateHttpStep(HttpResponseMessage? responseMessage = null, Expression<Func<HttpRequestMessage, bool>>? requestMessageRequirement = null, HttpStepConfiguration? configuration = null)
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
        var configElement = configuration ?? new("POST", "https://url.com");
        return new HttpStep(httpClient, configElement);
    }
}