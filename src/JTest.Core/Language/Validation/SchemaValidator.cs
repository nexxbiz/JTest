using System.Text.Json;
using Json.Schema;
using JTest.Core.Assertions;

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
            {
                // The schema cannot express the operator set: operators are matched case-insensitively
                // at run time, so a strict enum would reject spellings that execute fine. Resolve them
                // against the runtime's own registry instead (FR-055).
                var semantic = new List<LanguageDiagnostic>();
                ValidateAssertionOperators(document.RootElement, "", semantic);

                return semantic.Count == 0
                    ? new LanguageValidationResult(true, Array.Empty<LanguageDiagnostic>())
                    : new LanguageValidationResult(false, semantic);
            }

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

    /// <summary>
    /// Walks the definition and resolves every assertion operator — in an <c>assert</c> array or a
    /// <c>while</c> step's <c>condition</c> — reporting unknown ones with their JSON Pointer. Without
    /// this an operator typo passes validation and only surfaces at run time, as a suite that fails
    /// to load.
    /// </summary>
    private static void ValidateAssertionOperators(JsonElement element, string pointer, List<LanguageDiagnostic> diagnostics)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childPointer = $"{pointer}/{Escape(property.Name)}";

                    if (property.NameEquals("assert") && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var index = 0;
                        foreach (var operation in property.Value.EnumerateArray())
                            CheckOperator(operation, $"{childPointer}/{index++}", diagnostics);
                    }
                    else if (property.NameEquals("condition") && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        CheckOperator(property.Value, childPointer, diagnostics);
                    }

                    ValidateAssertionOperators(property.Value, childPointer, diagnostics);
                }
                break;

            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                    ValidateAssertionOperators(item, $"{pointer}/{i++}", diagnostics);
                break;
        }
    }

    private static void CheckOperator(JsonElement operation, string pointer, List<LanguageDiagnostic> diagnostics)
    {
        if (operation.ValueKind != JsonValueKind.Object) return;
        if (!operation.TryGetProperty("op", out var op) || op.ValueKind != JsonValueKind.String) return;

        var name = op.GetString();
        if (AssertionOperators.IsKnown(name)) return;

        diagnostics.Add(new LanguageDiagnostic(
            $"{pointer}/op",
            $"Unknown assertion operator '{name}'. Supported operators: {string.Join(", ", AssertionOperators.All)}."));
    }

    /// <summary>RFC 6901 escaping for a JSON Pointer segment.</summary>
    private static string Escape(string segment) => segment.Replace("~", "~0").Replace("/", "~1");

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
