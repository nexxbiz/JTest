using System.Text.Json;
using Json.Schema;

namespace JTest.Core.Language.Validation;

/// <summary>A single validation problem with its location in the definition (JSON Pointer).</summary>
public sealed record LanguageDiagnostic(string Location, string Message);

public sealed record LanguageValidationResult(bool IsValid, IReadOnlyList<LanguageDiagnostic> Diagnostics);

/// <summary>
/// Validates a JTest test-definition document against the authoritative, versioned JSON Schema
/// (FR-029/030). Returns machine-readable, located diagnostics (FR-031). This is real schema
/// enforcement, not a shallow structural probe.
/// </summary>
public sealed class SchemaValidator
{
    public const string SchemaVersion = "1.0.0";

    // Load/compile the schema exactly once (avoids duplicate $id registration in the global registry).
    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    public LanguageValidationResult Validate(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return new LanguageValidationResult(false, new[] { new LanguageDiagnostic("", $"Invalid JSON: {ex.Message}") });
        }

        using (document)
        {
            var results = Schema.Value.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (results.IsValid)
                return new LanguageValidationResult(true, Array.Empty<LanguageDiagnostic>());

            var diagnostics = new List<LanguageDiagnostic>();
            foreach (var detail in results.Details ?? Enumerable.Empty<EvaluationResults>())
            {
                if (detail.IsValid || detail.Errors is null) continue;
                foreach (var error in detail.Errors)
                    diagnostics.Add(new LanguageDiagnostic(detail.InstanceLocation.ToString(), error.Value));
            }

            if (diagnostics.Count == 0)
                diagnostics.Add(new LanguageDiagnostic("", "Definition does not conform to the JTest language schema."));

            return new LanguageValidationResult(false, diagnostics);
        }
    }

    private static JsonSchema LoadSchema()
    {
        var assembly = typeof(SchemaValidator).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("jtest-language-1.0.0.schema.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
