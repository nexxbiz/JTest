using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// HTTP step implementation for making HTTP requests
/// </summary>
public sealed class HttpStep(HttpClient httpClient, HttpStepConfiguration configuration) : BaseStep<HttpStepConfiguration>(configuration)
{    
    private const string jsonContentType = "application/json";

    public override async Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var responseData = await PerformHttpRequest(context);

        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = $"HTTP {ResolveStringValue(Configuration.Method, context)} {ResolveStringValue(Configuration.Url, context)}";
        }

        return responseData;
    }

    private async Task<object> PerformHttpRequest(IExecutionContext context)
    {
        var request = BuildHttpRequest(context);
        var requestDetails = await CaptureRequestDetails(request, context);
        var response = await httpClient.SendAsync(request);
        return await CreateResponseData(response, requestDetails);
    }

    private HttpRequestMessage BuildHttpRequest(IExecutionContext context)
    {
        var method = ResolveStringValue(Configuration.Method, context);
        var url = ResolveStringValue(Configuration.Url, context);
        var finalUrl = AddQueryParameters(url, context);
        var request = new HttpRequestMessage(new HttpMethod(method), finalUrl)
        {
            Content = GetRequestBodyContent(context)
        };
        AddResolvedHeaders(request, context);
        return request;
    }


    private string AddQueryParameters(string url, IExecutionContext context)
    {
        if (Configuration.Query is null || Configuration.Query.Count == 0)
        {
            return url;
        }

        var queryComponents = Configuration.Query.Select(q => BuildQueryComponent(q, context));
        var queryString = string.Join("&", queryComponents.Where(p => !string.IsNullOrEmpty(p)));

        return string.IsNullOrEmpty(queryString)
            ? url
            : $"{url}?{queryString}";
    }

    private static string BuildQueryComponent(KeyValuePair<string, string> query, IExecutionContext context)
    {
        var key = Uri.EscapeDataString(query.Key);
        var value = ResolveStringValue(query.Value, context);

        return string.IsNullOrEmpty(value)
            ? string.Empty
            : $"{key}={Uri.EscapeDataString(value)}";
    }

    private void AddResolvedHeaders(HttpRequestMessage request, IExecutionContext context)
    {
        if (Configuration.Headers is null || !Configuration.Headers.Any())
            return;

        foreach (var header in Configuration.Headers)
        {
            var name = ResolveStringValue(header.Name, context);
            var value = ResolveStringValue(header.Value, context);

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }
    }

    private HttpContent? GetRequestBodyContent(IExecutionContext context)
    {
        if (Configuration.Body is not null)
        {
            return CreateJsonStringContent(context);
        }

        else if (Configuration.FormFiles is not null && Configuration.FormFiles.Any())
        {
            return CreateMultipartFormDataContent(context);
        }

        else if (!string.IsNullOrWhiteSpace(Configuration.File))
        {
            return CreateFileStreamContent(context);
        }

        return null;
    }

    private StringContent? CreateJsonStringContent(IExecutionContext context)
    {
        var bodyElement = SerializeToJsonElement(Configuration.Body);
        var resolvedBody = ResolveJsonElement(bodyElement, context);
        var jsonString = SerializeBodyToJson(resolvedBody);
        return CreateStringContent(jsonString);
    }

    private StreamContent? CreateFileStreamContent(IExecutionContext context)
    {
        var filePath = ResolveStringValue(Configuration.File!, context);
        if (!File.Exists(filePath))
        {
            context.Log.Add($"File at path '{filePath}' does not eixst");
            return null;
        }

        var fileContent = File.OpenRead(filePath);
        var result = new StreamContent(fileContent);

        var contentType = !string.IsNullOrWhiteSpace(Configuration.ContentType)
            ? Configuration.ContentType
            : jsonContentType;

        result.Headers.ContentType = new MediaTypeHeaderValue(
            ResolveStringValue(contentType, context)
        );

        return result;
    }

    private MultipartFormDataContent CreateMultipartFormDataContent(IExecutionContext context)
    {
        var result = new MultipartFormDataContent();

        foreach (var formFile in Configuration.FormFiles!)
        {
            var streamContent = new StreamContent(
                File.OpenRead(formFile.Path)
            );
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
            result.Add(streamContent, formFile.Name, formFile.FileName);
        }

        return result;
    }


    private object ResolveJsonElement(JsonElement bodyElement, IExecutionContext context)
    {
        return bodyElement.ValueKind switch
        {
            JsonValueKind.String => ResolveStringValue(bodyElement.GetString()!, context),
            JsonValueKind.Object => ResolveObjectTokens(bodyElement, context),
            JsonValueKind.Array => ResolveArrayToken(bodyElement, context),
            _ => GetJsonElementValue(bodyElement)
        };
    }

    private Dictionary<string, object> ResolveObjectTokens(JsonElement objectElement, IExecutionContext context)
    {
        var resolved = new Dictionary<string, object>();
        foreach (var property in objectElement.EnumerateObject())
        {
            resolved[property.Name] = ResolveJsonElement(property.Value, context);
        }
        return resolved;
    }

    private List<object> ResolveArrayToken(JsonElement arrayElement, IExecutionContext context)
    {
        var resolved = new List<object>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            var resolvedItem = ResolveJsonElement(item, context);
            resolved.Add(resolvedItem);
        }
        return resolved;
    }

    private static object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Undefined => null!,
            _ => element.GetRawText()
        };
    }

    private static string SerializeBodyToJson(object body)
    {
        return body is string str ? str : JsonSerializer.Serialize(body, JsonSerializerOptionsCache.Default);
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

    private static async Task<object> CreateResponseData(HttpResponseMessage response, object requestDetails)
    {
        var body = await GetResponseBody(response);
        var headers = GetResponseHeaders(response);
        return new
        {
            status = (int)response.StatusCode,
            headers,
            body,
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
            return JsonSerializer.Deserialize<object>(content, JsonSerializerOptionsCache.Default) ?? content;
        }
        catch
        {
            return content;
        }
    }

    private static object[] GetResponseHeaders(HttpResponseMessage response)
    {
        var result = response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) });

        return [.. result];
    }
}