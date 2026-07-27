using System.Text.Json.Nodes;
using JTest.Language.Diagnostics;

namespace JTest.Engine.Expressions;

/// <summary>Outcome of load-time <c>${NAME}</c> substitution over a value map.</summary>
/// <param name="Values">The substituted values.</param>
/// <param name="SubstitutedPointers">JSON pointers of values that received process-environment content; these are sensitive by default.</param>
/// <param name="Diagnostics">Substitution failures.</param>
public sealed record EnvironmentSubstitutionResult(
    JsonObject Values,
    IReadOnlyList<string> SubstitutedPointers,
    IReadOnlyList<LanguageDiagnostic> Diagnostics)
{
    /// <summary>Whether every token substituted successfully.</summary>
    public bool Success => Diagnostics.Count == 0;
}
