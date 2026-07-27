using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;
using JTest.Engine.Redaction;
using JTest.Engine.Tracing;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;

namespace JTest.Engine.Execution;

/// <summary>
/// Executes http steps: resolves the request, sends it through the
/// transport, exposes the full exchange as the step result, and captures a
/// redacted evidence snapshot on the trace node.
/// </summary>
internal sealed class HttpStepRunner
{
    private readonly StepServices services;

    internal HttpStepRunner(StepServices services)
    {
        this.services = services;
    }

    internal async Task<JsonNode?> Execute(
        HttpStepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        if (!TryResolveText(step.Method, frame, node, out var method) ||
            !TryResolveText(step.Url, frame, node, out var url))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in step.Headers)
        {
            if (!TryResolveText(header.Value, frame, node, out var headerValue))
            {
                return null;
            }

            headers[header.Key] = headerValue;
        }

        var query = new List<(string Key, string Value)>();
        foreach (var parameter in step.Query)
        {
            if (!TryResolveText(parameter.Value, frame, node, out var parameterValue))
            {
                return null;
            }

            query.Add((parameter.Key, parameterValue));
        }

        JsonNode? body = null;
        if (step.Body is not null)
        {
            var resolved = ExpressionResolver.ResolveValue(step.Body.Value, frame, services.Source);
            if (!resolved.Success)
            {
                node.RecordOutcome(TraceOutcome.Failed);
                node.AddDiagnostic(resolved.Diagnostic!);
                return null;
            }

            body = resolved.Value;
        }

        var finalUrl = AppendQuery(url, query);
        using var request = new HttpRequestMessage(new HttpMethod(method), finalUrl);
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }
        else if (step.File is not null)
        {
            if (!TryResolveText(step.File, frame, node, out var filePath))
            {
                return null;
            }

            if (!File.Exists(filePath))
            {
                return FailMissingFile(node, filePath);
            }

            request.Content = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false));
            request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");
        }
        else if (step.FormFiles.Count > 0)
        {
            var form = new MultipartFormDataContent();
            foreach (var formFile in step.FormFiles)
            {
                if (!TryResolveText(formFile.Path, frame, node, out var formPath))
                {
                    form.Dispose();
                    return null;
                }

                if (!File.Exists(formPath))
                {
                    form.Dispose();
                    return FailMissingFile(node, formPath);
                }

                var content = new ByteArrayContent(await File.ReadAllBytesAsync(formPath, cancellationToken).ConfigureAwait(false));
                if (formFile.ContentType is not null)
                {
                    content.Headers.TryAddWithoutValidation("Content-Type", formFile.ContentType);
                }

                form.Add(content, formFile.Name, Path.GetFileName(formPath));
            }

            request.Content = form;
        }

        var requestEvidence = new JsonObject
        {
            ["method"] = method,
            ["url"] = Redactor.RedactText(finalUrl, services.Secrets),
            ["headers"] = RedactedHeaders(headers),
            ["body"] = body is null ? null : Redactor.Redact(body, services.Secrets),
        };

        var start = services.Clock.UtcNow;
        HttpResponseMessage response;
        try
        {
            using var timeout = LinkTimeout(step.TimeoutMs, cancellationToken, out var linkedToken);
            response = await services.Transport.SendAsync(request, linkedToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            node.RecordOutcome(TraceOutcome.TimedOut);
            node.Evidence = new JsonObject
            {
                ["request"] = requestEvidence,
                ["timedOutAfterMs"] = step.TimeoutMs,
            };
            return null;
        }
        catch (HttpRequestException exception)
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.Evidence = new JsonObject { ["request"] = requestEvidence };
            node.AddDiagnostic(new LanguageDiagnostic(
                RuntimeDiagnosticCodes.ValueTypeMismatch,
                DiagnosticSeverity.Error,
                $"The HTTP request could not be completed: {Redactor.RedactText(exception.Message, services.Secrets)}",
                services.Source,
                string.Empty));
            return null;
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var durationMs = (services.Clock.UtcNow - start).TotalMilliseconds;

            var responseHeaders = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            JsonNode? parsedBody = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    parsedBody = JsonNode.Parse(raw);
                }
            }
            catch (JsonException)
            {
                parsedBody = null;
            }

            var result = new JsonObject
            {
                ["request"] = new JsonObject { ["method"] = method, ["url"] = finalUrl },
                ["response"] = new JsonObject
                {
                    ["status"] = (int)response.StatusCode,
                    ["headers"] = ToJsonObject(responseHeaders),
                    ["body"] = parsedBody,
                    ["raw"] = raw,
                },
                ["durationMs"] = durationMs,
            };

            node.Evidence = new JsonObject
            {
                ["request"] = requestEvidence,
                ["response"] = new JsonObject
                {
                    ["status"] = (int)response.StatusCode,
                    ["headers"] = RedactedHeaders(responseHeaders),
                    ["body"] = parsedBody is null ? null : Redactor.Redact(parsedBody, services.Secrets),
                    ["raw"] = parsedBody is null ? Redactor.RedactText(raw, services.Secrets) : null,
                },
                ["durationMs"] = durationMs,
            };

            return result;
        }
    }

    private JsonNode? FailMissingFile(TraceNode node, string filePath)
    {
        node.RecordOutcome(TraceOutcome.Failed);
        node.AddDiagnostic(new LanguageDiagnostic(
            RuntimeDiagnosticCodes.ValueTypeMismatch,
            DiagnosticSeverity.Error,
            $"No file exists at path '{filePath}'.",
            services.Source,
            string.Empty));
        return null;
    }

    private bool TryResolveText(string template, ExecutionFrame frame, TraceNode node, out string text)
    {
        text = string.Empty;
        var resolved = ExpressionResolver.ResolveString(template, frame, services.Source);
        if (!resolved.Success)
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.AddDiagnostic(resolved.Diagnostic!);
            return false;
        }

        text = ExpressionResolver.Stringify(resolved.Value);
        return true;
    }

    private JsonObject RedactedHeaders(IReadOnlyDictionary<string, string> headers)
    {
        var result = new JsonObject();
        foreach (var header in headers)
        {
            result[header.Key] = Redactor.RedactHeader(header.Key, header.Value, services.Secrets);
        }

        return result;
    }

    private static JsonObject ToJsonObject(IReadOnlyDictionary<string, string> headers)
    {
        var result = new JsonObject();
        foreach (var header in headers)
        {
            result[header.Key] = header.Value;
        }

        return result;
    }

    private static string AppendQuery(string url, List<(string Key, string Value)> query)
    {
        if (query.Count == 0)
        {
            return url;
        }

        var components = query.Select(static p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{separator}{string.Join('&', components)}";
    }

    private static CancellationTokenSource? LinkTimeout(
        double? timeoutMs,
        CancellationToken cancellationToken,
        out CancellationToken linkedToken)
    {
        if (timeoutMs is null)
        {
            linkedToken = cancellationToken;
            return null;
        }

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs.Value));
        linkedToken = source.Token;
        return source;
    }
}
