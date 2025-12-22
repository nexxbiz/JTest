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

    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        if (OnlyOneBodyTypeDefined() == false)
        {
            validationErrors.Add("You can only specify 1 body type. Choose only of the following: 'body', 'file', or 'formFiles'.");
        }

        var file = ResolveStringVariable(Configuration.File, context);
        if (!string.IsNullOrWhiteSpace(file) && !File.Exists(file))
        {
            validationErrors.Add($"No file found at path '{file}'.");
        }
        if (Configuration.FormFiles?.Any() == true)
        {
            foreach (var formFile in Configuration.FormFiles)
            {
                var path = ResolveStringVariable(formFile.Path, context);
                if (!File.Exists(path))
                {
                    validationErrors.Add($"No file found at path '{path}'.");
                }
            }
        }

        var method = ResolveStringVariable(Configuration.Method, context);
        try
        {
            _ = new HttpMethod(method);
        }
        catch (Exception)
        {
            validationErrors.Add($"Invalid HTTP Method '{method}'");
        }

        var uri = ResolveStringVariable(Configuration.Url, context);
        try
        {
            _ = new Uri(uri);
        }
        catch (Exception)
        {
            validationErrors.Add($"Invalid url '{uri}'");
        }
    }

    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var responseData = await PerformHttpRequest(context);

        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = $"HTTP {ResolveVariable(Configuration.Method, context)} {ResolveVariable(Configuration.Url, context)}";
        }

        return new(responseData);
    }

    private async Task<Dictionary<string, object?>> PerformHttpRequest(IExecutionContext context)
    {
        var request = BuildHttpRequest(context);
        var requestDetails = await CaptureRequestDetails(request, context);
        var response = await httpClient.SendAsync(request);
        return await CreateResponseData(response, requestDetails);
    }

    private HttpRequestMessage BuildHttpRequest(IExecutionContext context)
    {
        var url = ResolveStringVariable(Configuration.Url, context);
        var finalUrl = AddQueryParameters(url, context);

        var method = ResolveStringVariable(Configuration.Method, context);
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
        var value = ResolveStringVariable(query.Value, context);

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
            var name = ResolveStringVariable(header.Name, context);
            var value = ResolveStringVariable(header.Value, context);

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
        var filePath = ResolveStringVariable(Configuration.File!, context);
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
            ResolveStringVariable(contentType, context)
        );

        return result;
    }

    private MultipartFormDataContent CreateMultipartFormDataContent(IExecutionContext context)
    {
        var result = new MultipartFormDataContent();

        foreach (var formFile in Configuration.FormFiles!)
        {
            var path = $"{ResolveVariable(formFile.Path, context)}";
            var streamContent = new StreamContent(
                File.OpenRead(path)
            );
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                $"{ResolveVariable(formFile.ContentType, context)}"
            );
            result.Add(
                streamContent,
                ResolveStringVariable(formFile.Name, context),
                ResolveStringVariable(formFile.FileName, context)
            );
        }

        return result;
    }


    private object? ResolveJsonElement(JsonElement bodyElement, IExecutionContext context)
    {
        return bodyElement.ValueKind switch
        {
            JsonValueKind.String => ResolveVariable(bodyElement.GetString()!, context),
            JsonValueKind.Object => ResolveObjectTokens(bodyElement, context),
            JsonValueKind.Array => ResolveArrayToken(bodyElement, context),
            _ => GetJsonElementValue(bodyElement)
        };
    }

    private Dictionary<string, object?> ResolveObjectTokens(JsonElement objectElement, IExecutionContext context)
    {
        var resolved = new Dictionary<string, object?>();
        foreach (var property in objectElement.EnumerateObject())
        {
            resolved[property.Name] = ResolveJsonElement(property.Value, context);
        }
        return resolved;
    }

    private List<object?> ResolveArrayToken(JsonElement arrayElement, IExecutionContext context)
    {
        var resolved = new List<object?>();
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

    private static string SerializeBodyToJson(object? body)
    {
        return body is string str ? str : JsonSerializer.Serialize(body, JsonSerializerOptionsAccessor.Default);
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

        return new Dictionary<string, object?>
        {
            ["url"] = request.RequestUri?.ToString() ?? string.Empty,
            ["method"] = request.Method.Method,
            ["headers"] = headers,
            ["body"] = body
        };
    }

    private static async Task<Dictionary<string, object?>> CreateResponseData(HttpResponseMessage response, object requestDetails)
    {
        var body = await GetResponseBody(response);
        var headers = GetResponseHeaders(response);
        return new Dictionary<string, object?>
        {
            ["status"] = (int)response.StatusCode,
            ["headers"] = headers,
            ["body"] = body,
            ["request"] = requestDetails
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
            return JsonSerializer.Deserialize<object>(content, JsonSerializerOptionsAccessor.Default) ?? content;
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

    private bool? OnlyOneBodyTypeDefined()
    {
        if (!string.IsNullOrWhiteSpace(Configuration.File))
        {
            return Configuration.Body is null && Configuration.FormFiles?.Any() != true;
        }

        if (Configuration.Body is not null)
        {
            return string.IsNullOrWhiteSpace(Configuration.File) && Configuration.FormFiles?.Any() != true;
        }

        if (Configuration.FormFiles?.Any() == true)
        {
            return Configuration.Body is null && string.IsNullOrWhiteSpace(Configuration.File);
        }

        return null;
    }
}