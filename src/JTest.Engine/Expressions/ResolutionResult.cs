using System.Text.Json.Nodes;
using JTest.Language.Diagnostics;

namespace JTest.Engine.Expressions;

/// <summary>Outcome of resolving one expression or value template.</summary>
/// <param name="Value">The resolved value; meaningful only on success.</param>
/// <param name="Diagnostic">The failure diagnostic; null on success.</param>
public sealed record ResolutionResult(JsonNode? Value, LanguageDiagnostic? Diagnostic)
{
    /// <summary>Whether resolution succeeded.</summary>
    public bool Success => Diagnostic is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The resolved value.</param>
    public static ResolutionResult Ok(JsonNode? value) => new(value, null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public static ResolutionResult Fail(LanguageDiagnostic diagnostic) => new(null, diagnostic);
}
