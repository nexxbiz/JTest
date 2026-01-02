using JTest.Core.Debugging;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.Core.Output;

public sealed class HttpStepResultDataWriter
{
    private readonly SecurityMasker _securityMasker = new();

    public void WriteData(TextWriter writer, StepProcessedResult httpStepResult)
    {
        if (httpStepResult.Step is not HttpStep)
        {
            return;
        }
        if (httpStepResult.Data is null)
        {
            return;
        }

        WriteHttpRequestDetails(writer, httpStepResult);
    }

    private void WriteHttpRequestDetails(TextWriter writer, StepProcessedResult step)
    {
        // Only show HTTP request details for HTTP steps
        if (step.Step.TypeName != "http" || step.Data == null) return;

        var requestData = ExtractRequestDetails(step.Data);
        if (requestData is null)
            return;

        writer.WriteLine();
        writer.WriteLine("**HTTP request details:**");
        writer.WriteLine();

        // Use HTML table for HTTP request details
        writer.WriteLine("<table>");
        writer.WriteLine("<thead>");
        writer.WriteLine("<tr><th>Field</th><th>Value</th></tr>");
        writer.WriteLine("</thead>");
        writer.WriteLine("<tbody>");

        // Add URL
        var url = System.Net.WebUtility.HtmlEncode($"{requestData["Url"]}");
        writer.WriteLine($"<tr><td>URL</td><td>{url}</td></tr>");

        // Add Method
        var method = System.Net.WebUtility.HtmlEncode($"{requestData["Method"]}");
        writer.WriteLine($"<tr><td>Method</td><td>{method}</td></tr>");

        // Add Headers
        if (requestData["Headers"] != null && requestData["Headers"] is IEnumerable<object> headers && headers.Any())
        {
            var headersDisplay = FormatHttpHeaders([.. headers]);
            writer.WriteLine($"<tr><td>Headers</td><td>{headersDisplay}</td></tr>");
        }

        // Add Body
        if (!string.IsNullOrEmpty($"{requestData["Body"]}"))
        {
            var bodyDisplay = FormatHttpBody($"{requestData["Body"]}");
            writer.WriteLine($"<tr><td>Body</td><td>{bodyDisplay}</td></tr>");
        }

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table>");
        writer.WriteLine();
    }

    static private Dictionary<string, object?>? ExtractRequestDetails(object stepData)
    {
        try
        {
            var dataType = stepData.GetType();
            var requestProperty = dataType.GetProperty("request");
            if (requestProperty == null) return null;

            var requestData = requestProperty.GetValue(stepData);
            if (requestData == null) return null;

            var requestType = requestData.GetType();
            var url = requestType.GetProperty("url")?.GetValue(requestData)?.ToString();
            var method = requestType.GetProperty("method")?.GetValue(requestData)?.ToString();
            var headers = requestType.GetProperty("headers")?.GetValue(requestData) as object[];
            var body = requestType.GetProperty("body")?.GetValue(requestData)?.ToString();

            return new Dictionary<string, object?>
            {
                ["Url"] = url,
                ["Method"] = method,
                ["Headers"] = headers,
                ["Body"] = body
            };
        }
        catch
        {
            // If we can't extract request details, just don't show them
            return null;
        }
    }

    private string FormatHttpHeaders(object[] headers)
    {
        try
        {
            var headerStrings = new List<string>();
            foreach (var header in headers)
            {
                var headerType = header.GetType();
                var name = headerType.GetProperty("name")?.GetValue(header)?.ToString();
                var value = headerType.GetProperty("value")?.GetValue(header)?.ToString();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    // Register sensitive headers for masking
                    if (IsSensitiveHeader(name))
                    {
                        _securityMasker.RegisterForMasking(name, value);
                    }

                    var encodedName = System.Net.WebUtility.HtmlEncode(name);
                    var encodedValue = System.Net.WebUtility.HtmlEncode(_securityMasker.ApplyMasking(value));
                    headerStrings.Add($"{encodedName}: {encodedValue}");
                }
            }

            if (headerStrings.Count == 0) return "None";

            return $"<pre>{string.Join("<br/>", headerStrings)}</pre>";
        }
        catch
        {
            return "Unable to display headers";
        }
    }

    private string FormatHttpBody(string body)
    {
        if (string.IsNullOrEmpty(body)) return "Empty";

        // Register body for masking in case it contains sensitive data
        _securityMasker.RegisterForMasking("requestBody", body);

        // Try to format as JSON if it looks like JSON
        if (IsJsonString(body))
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
                var formatted = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                var encoded = System.Net.WebUtility.HtmlEncode(formatted);
                return $"<details><summary>show JSON</summary><pre>{encoded}</pre></details>";
            }
            catch
            {
                // Fall back to plain text display
            }
        }

        // Display as plain text
        var encodedBody = System.Net.WebUtility.HtmlEncode(body);
        return $"<pre>{encodedBody}</pre>";
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[] { "authorization", "x-api-key", "x-auth-token", "cookie", "set-cookie" };
        return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static bool IsJsonString(string str)
    {
        str = str.Trim();
        return (str.StartsWith('{') && str.EndsWith('}')) || (str.StartsWith('[') && str.EndsWith(']'));
    }
}
