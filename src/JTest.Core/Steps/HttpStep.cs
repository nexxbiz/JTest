using JTest.Core.Execution;
using JTest.Core.Utilities;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// HTTP step implementation for making HTTP requests
/// </summary>
public class HttpStep : BaseStep
{
    private readonly HttpClient _httpClient;
    private string _stepDescription = "";

    public HttpStep(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override string Type => "http";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        SetConfiguration(configuration);
        return ValidateRequiredProperties();
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
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
        return Configuration.TryGetProperty("method", out _) &&
               Configuration.TryGetProperty("url", out _);
    }

    private async Task<object> PerformHttpRequest(IExecutionContext context, Stopwatch stopwatch)
    {
        var request = BuildHttpRequest(context);
        var requestDetails = await CaptureRequestDetails(request, context);
        var response = await _httpClient.SendAsync(request);
        return await CreateResponseData(response, stopwatch, requestDetails);
    }

    private HttpRequestMessage BuildHttpRequest(IExecutionContext context)
    {
        var method = GetResolvedMethod(context);
        var url = GetResolvedUrl(context);
        var finalUrl = AddQueryParameters(url, context);
        var request = new HttpRequestMessage(new HttpMethod(method), finalUrl);
        AddResolvedHeaders(request, context);
        AddResolvedBody(request, context);
        return request;
    }

    private string GetResolvedMethod(IExecutionContext context)
    {
        var method = Configuration.GetProperty("method").GetString() ?? "GET";
        return VariableInterpolator.ResolveVariableTokens(method, context).ToString() ?? "GET";
    }

    private string GetResolvedUrl(IExecutionContext context)
    {
        var url = Configuration.GetProperty("url").GetString() ?? "";
        return VariableInterpolator.ResolveVariableTokens(url, context).ToString() ?? "";
    }

    private string AddQueryParameters(string url, IExecutionContext context)
    {
        if (!Configuration.TryGetProperty("query", out var queryElement)) return url;
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

    private string BuildQueryParameter(JsonProperty property, IExecutionContext context)
    {
        var key = Uri.EscapeDataString(property.Name);
        var value = ResolveQueryValue(property.Value, context);
        return string.IsNullOrEmpty(value) ? "" : $"{key}={Uri.EscapeDataString(value)}";
    }

    private string ResolveQueryValue(JsonElement value, IExecutionContext context)
    {
        if (value.ValueKind == JsonValueKind.String)
            return VariableInterpolator.ResolveVariableTokens(value.GetString() ?? "", context).ToString() ?? "";
        return GetJsonElementValue(value).ToString() ?? "";
    }

    private void AddResolvedHeaders(HttpRequestMessage request, IExecutionContext context)
    {
        if (!Configuration.TryGetProperty("headers", out var headersElement)) return;
        if (headersElement.ValueKind != JsonValueKind.Array) return;
        foreach (var header in headersElement.EnumerateArray())
        {
            AddSingleHeader(request, header, context);
        }
    }

    private void AddSingleHeader(HttpRequestMessage request, JsonElement header, IExecutionContext context)
    {
        var name = GetHeaderName(header, context);
        var value = GetHeaderValue(header, context);
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            request.Headers.TryAddWithoutValidation(name, value);
    }

    private string GetHeaderName(JsonElement header, IExecutionContext context)
    {
        var name = header.GetProperty("name").GetString() ?? "";
        return VariableInterpolator.ResolveVariableTokens(name, context).ToString() ?? "";
    }

    private string GetHeaderValue(JsonElement header, IExecutionContext context)
    {
        var value = header.GetProperty("value").GetString() ?? "";
        return VariableInterpolator.ResolveVariableTokens(value, context).ToString() ?? "";
    }

    private void AddResolvedBody(HttpRequestMessage request, IExecutionContext context)
    {
        if (!Configuration.TryGetProperty("body", out var bodyElement)) return;
        var content = CreateHttpContent(bodyElement, context);
        if (content != null) request.Content = content;
    }

    private HttpContent? CreateHttpContent(JsonElement bodyElement, IExecutionContext context)
    {
        var resolvedBody = ResolveBodyTokens(bodyElement, context);
        var jsonString = SerializeBodyToJson(resolvedBody);
        return CreateStringContent(jsonString);
    }

    private object ResolveBodyTokens(JsonElement bodyElement, IExecutionContext context)
    {
        return bodyElement.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(bodyElement.GetString() ?? "", context),
            JsonValueKind.Object => ResolveObjectTokens(bodyElement, context),
            _ => bodyElement
        };
    }

    private object ResolveObjectTokens(JsonElement objectElement, IExecutionContext context)
    {
        var resolved = new Dictionary<string, object>();
        foreach (var property in objectElement.EnumerateObject())
        {
            resolved[property.Name] = ResolvePropertyValue(property.Value, context);
        }
        return resolved;
    }

    private object ResolvePropertyValue(JsonElement value, IExecutionContext context)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(value.GetString() ?? "", context),
            JsonValueKind.Object => ResolveObjectTokens(value, context),
            _ => GetJsonElementValue(value)
        };
    }

    private object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => element.GetRawText()
        };
    }

    private string SerializeBodyToJson(object body)
    {
        return body is string str ? str : JsonSerializer.Serialize(body);
    }

    private HttpContent CreateStringContent(string content)
    {
        return new StringContent(content, Encoding.UTF8, "application/json");
    }

    private async Task<object> CaptureRequestDetails(HttpRequestMessage request, IExecutionContext context)
    {
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) })
            .ToArray();

        var body = request.Content != null 
            ? await request.Content.ReadAsStringAsync()
            : null;

        return new
        {
            url = request.RequestUri?.ToString() ?? "",
            method = request.Method.Method,
            headers = headers,
            body = body
        };
    }

    private async Task<object> CreateResponseData(HttpResponseMessage response, Stopwatch stopwatch, object requestDetails)
    {
        var body = await GetResponseBody(response);
        var headers = GetResponseHeaders(response);
        return new
        {
            status = (int)response.StatusCode,
            headers = headers,
            body = body,
            duration = stopwatch.ElapsedMilliseconds,
            request = requestDetails
        };
    }

    private async Task<object> GetResponseBody(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return IsJsonResponse(response) ? ParseJsonResponse(content) : content;
    }

    private bool IsJsonResponse(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return contentType?.Contains("application/json") == true;
    }

    private object ParseJsonResponse(string content)
    {
        try { return JsonSerializer.Deserialize<object>(content) ?? content; }
        catch { return content; }
    }

    private object[] GetResponseHeaders(HttpResponseMessage response)
    {
        return response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) })
            .ToArray();
    }

    public override string GetStepDescription()
    {
        return _stepDescription;
    }
}

internal static class TypeExtensions
{
    public static bool IsAnonymousType(this Type type)
    {
        return type.Name.Contains("AnonymousType");
    }
}