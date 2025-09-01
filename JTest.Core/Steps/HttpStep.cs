using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

/// <summary>
/// HTTP step implementation for making HTTP requests
/// </summary>
public class HttpStep : BaseStep
{
    private readonly HttpClient _httpClient;
    private readonly IDebugLogger? _debugLogger;

    public HttpStep(HttpClient httpClient, IDebugLogger? debugLogger = null)
    {
        _httpClient = httpClient;
        _debugLogger = debugLogger;
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
            StoreResultInContext(context, responseData);
            
            // Process assertions after storing response data
            var assertionResults = await ProcessAssertionsAsync(context);
            
            LogDebugInformation(context, contextBefore, stopwatch, true);
            var result = StepResult.CreateSuccess(responseData, stopwatch.ElapsedMilliseconds);
            result.AssertionResults = assertionResults;
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Log.Add($"HTTP request failed: {ex.Message}");
            LogDebugInformation(context, contextBefore, stopwatch, false);
            return StepResult.CreateFailure(ex.Message, stopwatch.ElapsedMilliseconds);
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
        var response = await _httpClient.SendAsync(request);
        return await CreateResponseData(response, stopwatch);
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

    private async Task<object> CreateResponseData(HttpResponseMessage response, Stopwatch stopwatch)
    {
        var body = await GetResponseBody(response);
        var headers = GetResponseHeaders(response);
        return new
        {
            status = (int)response.StatusCode,
            headers = headers,
            body = body,
            duration = stopwatch.ElapsedMilliseconds
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

    private Dictionary<string, object> CloneContext(IExecutionContext context)
    {
        return new Dictionary<string, object>(context.Variables);
    }

    private void LogDebugInformation(IExecutionContext context, Dictionary<string, object> contextBefore, Stopwatch stopwatch, bool success)
    {
        if (_debugLogger == null) return;
        
        var stepInfo = CreateStepDebugInfo(stopwatch, success);
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);
        
        _debugLogger.LogStepExecution(stepInfo);
        _debugLogger.LogContextChanges(contextChanges);
        _debugLogger.LogRuntimeContext(context.Variables);
    }

    private StepDebugInfo CreateStepDebugInfo(Stopwatch stopwatch, bool success)
    {
        return new StepDebugInfo
        {
            TestNumber = 1, // TODO: Get from context
            StepNumber = 1, // TODO: Get from context  
            StepType = "HttpStep",
            StepId = Id ?? "",
            Enabled = true,
            Result = success ? "✅ Success" : "❌ Failed",
            Duration = stopwatch.Elapsed,
            Description = ""
        };
    }

    private ContextChanges DetectContextChanges(Dictionary<string, object> before, Dictionary<string, object> after)
    {
        var changes = new ContextChanges();
        
        DetectAddedVariables(before, after, changes);
        DetectModifiedVariables(before, after, changes);
        GenerateAvailableExpressions(after, changes);
        
        return changes;
    }

    private void DetectAddedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (!before.ContainsKey(kvp.Key))
            {
                var description = DescribeValue(kvp.Value);
                changes.Added.Add($"`$.{kvp.Key}` = {description}");
            }
        }
    }

    private void DetectModifiedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (before.ContainsKey(kvp.Key) && !object.Equals(before[kvp.Key], kvp.Value))
            {
                var beforeDesc = DescribeValue(before[kvp.Key]);
                var afterDesc = DescribeValue(kvp.Value);
                changes.Modified.Add($"`$.{kvp.Key}`: {beforeDesc} → {afterDesc}");
            }
        }
    }

    private void GenerateAvailableExpressions(Dictionary<string, object> context, ContextChanges changes)
    {
        foreach (var key in context.Keys)
        {
            changes.Available.Add($"$.{key}");
        }
    }

    private string DescribeValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        
        // Handle anonymous types and complex objects
        var type = value.GetType();
        if (type.IsAnonymousType() || value is IDictionary<string, object> || value is Dictionary<string, object>)
        {
            // Try to count properties if possible
            if (value is IDictionary<string, object> dict)
                return $"{{object with {dict.Count} properties}}";
            
            // For anonymous types, estimate property count
            var properties = type.GetProperties();
            return $"{{object with {properties.Length} properties}}";
        }
        
        return value.ToString() ?? "unknown";
    }
}

internal static class TypeExtensions
{
    public static bool IsAnonymousType(this Type type)
    {
        return type.Name.Contains("AnonymousType");
    }
}