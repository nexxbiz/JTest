using System.Text.Json;
using JTest.Language.Diagnostics;

namespace JTest.Language.Reading;

/// <summary>Default fail-closed template file reader.</summary>
public sealed class TemplateFileReader : ITemplateFileReader
{
    /// <inheritdoc />
    public TemplateFileReadResult Read(string sourceName, string json)
    {
        var sink = new List<LanguageDiagnostic>();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                Diag.Error(sink, DiagnosticCodes.RootNotObject, "The document root must be a JSON object.", sourceName, string.Empty);
                return new TemplateFileReadResult(null, sink);
            }

            var bound = TemplateBinder.Bind(document.RootElement, sourceName, sink);
            return new TemplateFileReadResult(
                sink.Any(static d => d.Severity == DiagnosticSeverity.Error) ? null : bound,
                sink);
        }
        catch (JsonException exception)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.InvalidJson,
                $"The document is not valid JSON: {exception.Message}",
                sourceName,
                string.Empty);
            return new TemplateFileReadResult(null, sink);
        }
    }
}
