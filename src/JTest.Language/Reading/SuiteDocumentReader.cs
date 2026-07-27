using System.Text.Json;
using JTest.Language.Diagnostics;

namespace JTest.Language.Reading;

/// <summary>Default fail-closed suite reader.</summary>
public sealed class SuiteDocumentReader : ISuiteDocumentReader
{
    /// <inheritdoc />
    public SuiteReadResult Read(string sourceName, string json)
    {
        var sink = new List<LanguageDiagnostic>();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                Diag.Error(sink, DiagnosticCodes.RootNotObject, "The document root must be a JSON object.", sourceName, string.Empty);
                return new SuiteReadResult(null, sink);
            }

            var bound = SuiteBinder.Bind(document.RootElement, sourceName, sink);
            return new SuiteReadResult(
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
            return new SuiteReadResult(null, sink);
        }
    }
}
