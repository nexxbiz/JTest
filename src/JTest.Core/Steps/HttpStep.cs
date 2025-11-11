using JTest.Core.Execution;
using JTest.Core.Utilities;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// HTTP step implementation for making HTTP requests
/// </summary>
public class HttpStep(HttpClient httpClient) : BaseStep
{
    private const string stepType = "http";
    private const string defaultHttpMethod = "GET";
    private const string methodConfigurationProperty = "method";
    private const string urlConfigurationProperty = "url";
    private const string bodyConfigurationProperty = "body";
    private const string headersConfigurationProperty = "headers";
    private const string queryConfigurationProperty = "query";
    private const string fileConfigurationProperty = "file";
    private const string contentTypeConfigurationProperty = "contentType";
    private const string jsonContentType = "application/json";

    private string _stepDescription = string.Empty;

    public override sealed string Type => stepType;

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        SetConfiguration(configuration);
        return ValidateRequiredProperties();
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var responseData = await PerformHttpRequest(context, stopwatch);
            stopwatch.Stop();

            // Use common step completion logic from BaseStep
            var result = await ProcessStepCompletionAsync(context, contextBefore, stopwatch, responseData);

            _stepDescription = $"HTTP {GetResolvedMethod(context)} {GetResolvedUrl(context)}";
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Log.Add($"HTTP request failed: {ex.Message}");

            // Still process assertions even when HTTP request fails - this provides valuable debugging info
            var assertionResults = await ProcessAssertionsAsync(context);

            var result = StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
            result.AssertionResults = assertionResults;
            return result;
        }
    }

    private bool ValidateRequiredProperties()
    {
        return Configuration.TryGetProperty(methodConfigurationProperty, out _) &&
               Configuration.TryGetProperty(urlConfigurationProperty, out _);
    }

    private async Task<object> PerformHttpRequest(IExecutionContext context, Stopwatch stopwatch)
    {
        var request = BuildHttpRequest(context);
        var requestDetails = await CaptureRequestDetails(request, context);
        var response = await httpClient.SendAsync(request);
        return await CreateResponseData(response, stopwatch, requestDetails);
    }

    private HttpRequestMessage BuildHttpRequest(IExecutionContext context)
    {
        var method = GetResolvedMethod(context);
        var url = GetResolvedUrl(context);
        var finalUrl = AddQueryParameters(url, context);
        var request = new HttpRequestMessage(new HttpMethod(method), finalUrl)
        {
            Content = GetRequestBodyContent(context)
        };
        AddResolvedHeaders(request, context);
        return request;
    }

    private string GetResolvedMethod(IExecutionContext context)
    {
        var method = Configuration.GetProperty(methodConfigurationProperty).GetString() ?? defaultHttpMethod;
        return VariableInterpolator.ResolveVariableTokens(method, context).ToString() ?? defaultHttpMethod;
    }

    private string GetResolvedUrl(IExecutionContext context)
    {
        var url = Configuration.GetProperty(urlConfigurationProperty).GetString() ?? string.Empty;
        return VariableInterpolator.ResolveVariableTokens(url, context).ToString() ?? string.Empty;
    }

    private string AddQueryParameters(string url, IExecutionContext context)
    {
        if (!Configuration.TryGetProperty(queryConfigurationProperty, out var queryElement)) return url;
        if (queryElement.ValueKind != JsonValueKind.Object) return url;
        var queryString = BuildQueryString(queryElement, context);
        return string.IsNullOrEmpty(queryString) ? url : $"{url}?{queryString}";
    }

    private string BuildQueryString(JsonElement queryElement, IExecutionContext context)
    {
        var parameters = new List<string>();
        foreach (var property in queryElement.EnumerateObject())
            parameters.Add(BuildQueryParameter(property, context));
        return string.Join("&", parameters.Where(p => !string.IsNullOrEmpty(p)));
    }

    private static string BuildQueryParameter(JsonProperty property, IExecutionContext context)
    {
        var key = Uri.EscapeDataString(property.Name);
        var value = ResolveQueryValue(property.Value, context);
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : $"{key}={Uri.EscapeDataString(value)}";
    }

    private static string ResolveQueryValue(JsonElement value, IExecutionContext context)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return VariableInterpolator.ResolveVariableTokens(value.GetString() ?? string.Empty, context).ToString() ?? string.Empty;
        }

        return GetJsonElementValue(value).ToString() ?? string.Empty;
    }

    private void AddResolvedHeaders(HttpRequestMessage request, IExecutionContext context)
    {
        if (!Configuration.TryGetProperty(headersConfigurationProperty, out var headersElement))
            return;
        if (headersElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (var header in headersElement.EnumerateArray())
        {
            AddSingleHeader(request, header, context);
        }
    }

    private static void AddSingleHeader(HttpRequestMessage request, JsonElement header, IExecutionContext context)
    {
        var name = GetHeaderName(header, context);
        var value = GetHeaderValue(header, context);

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string GetHeaderName(JsonElement header, IExecutionContext context)
    {
        var name = header.GetProperty("name").GetString() ?? string.Empty;
        return VariableInterpolator.ResolveVariableTokens(name, context).ToString() ?? string.Empty;
    }

    private static string GetHeaderValue(JsonElement header, IExecutionContext context)
    {
        var value = header.GetProperty("value").GetString() ?? string.Empty;
        return VariableInterpolator.ResolveVariableTokens(value, context).ToString() ?? string.Empty;
    }

    private HttpContent? GetRequestBodyContent(IExecutionContext context)
    {
        if (Configuration.TryGetProperty(bodyConfigurationProperty, out var bodyElement))
        {
            return CreateJsonStringContent(bodyElement, context);
        }

        else if (Configuration.TryGetProperty(fileConfigurationProperty, out var filePath))
        {
            return CreateFileStreamContent(filePath, context);
        }

        return null;
    }

    private StringContent? CreateJsonStringContent(JsonElement bodyElement, IExecutionContext context)
    {
        var resolvedBody = ResolveBodyTokens(bodyElement, context);
        var jsonString = SerializeBodyToJson(resolvedBody);
        return CreateStringContent(jsonString);
    }

    private StreamContent? CreateFileStreamContent(JsonElement filePathElement, IExecutionContext context)
    {
        var filePath = $"{ResolvePropertyValue(filePathElement, context)}";
        if (!File.Exists(filePath))
        {
            context.Log.Add($"File at path '{filePath}' does not eixst");
            return null;
        }

        var fileContent = File.OpenRead(filePath);
        var result = new StreamContent(fileContent);

        if (Configuration.TryGetProperty(contentTypeConfigurationProperty, out var contentType))
        {
            result.Headers.ContentType = new MediaTypeHeaderValue($"{ResolvePropertyValue(contentType, context)}");
        }
        else
        {
            context.Log.Add($"No content type specified; default {jsonContentType} content type is applied");
            result.Headers.ContentType = new MediaTypeHeaderValue(jsonContentType);
        }

        return result;
    }

    private object ResolveBodyTokens(JsonElement bodyElement, IExecutionContext context)
    {
        return bodyElement.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(bodyElement.GetString() ?? string.Empty, context),
            JsonValueKind.Object => ResolveObjectTokens(bodyElement, context),
            JsonValueKind.Array => ResolveArrayToken(bodyElement, context),
            _ => bodyElement
        };
    }

    private Dictionary<string, object> ResolveObjectTokens(JsonElement objectElement, IExecutionContext context)
    {
        var resolved = new Dictionary<string, object>();
        foreach (var property in objectElement.EnumerateObject())
        {
            resolved[property.Name] = ResolvePropertyValue(property.Value, context);
        }
        return resolved;
    }

    private object ResolveArrayToken(JsonElement arrayElement, IExecutionContext context)
    {
        var resolved = new List<object>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            var resolvedItem = ResolvePropertyValue(item, context);
            resolved.Add(resolvedItem);
        }
        return resolved;
    }

    private object ResolvePropertyValue(JsonElement value, IExecutionContext context)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(value.GetString() ?? string.Empty, context),
            JsonValueKind.Object => ResolveObjectTokens(value, context),
            JsonValueKind.Array => ResolveArrayToken(value, context),
            _ => GetJsonElementValue(value)
        };
    }

    private static object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => element.GetRawText()
        };
    }

    private static string SerializeBodyToJson(object body)
    {
        return body is string str ? str : JsonSerializer.Serialize(body);
    }

    private static StringContent CreateStringContent(string content)
    {
        return new StringContent(content, Encoding.UTF8, jsonContentType);
    }

    private async Task<object> CaptureRequestDetails(HttpRequestMessage request, IExecutionContext context)
    {
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) })
            .ToArray();

        var bodyContent = GetRequestBodyContent(context);
        var body = bodyContent is not null
            ? await bodyContent.ReadAsStringAsync()
            : null;

        return new
        {
            url = request.RequestUri?.ToString() ?? string.Empty,
            method = request.Method.Method,
            headers,
            body
        };
    }

    private static async Task<object> CreateResponseData(HttpResponseMessage response, Stopwatch stopwatch, object requestDetails)
    {
        var body = await GetResponseBody(response);
        var headers = GetResponseHeaders(response);
        return new
        {
            status = (int)response.StatusCode,
            headers,
            body,
            duration = stopwatch.ElapsedMilliseconds,
            request = requestDetails
        };
    }

    private static async Task<object> GetResponseBody(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return IsJsonResponse(response) ? ParseJsonResponse(content) : content;
    }

    private static bool IsJsonResponse(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return contentType?.Contains(jsonContentType) == true;
    }

    private static object ParseJsonResponse(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(content) ?? content;
        }
        catch
        {
            return content;
        }
    }

    private static object[] GetResponseHeaders(HttpResponseMessage response)
    {
        return response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) })
            .ToArray();
    }

    public override string GetStepDescription() => _stepDescription;
}

internal static class TypeExtensions
{
    public static bool IsAnonymousType(this Type type)
    {
        return type.Name.Contains("AnonymousType");
    }
}