using JTest.Language.Diagnostics;
using JTest.Language.Documents;

namespace JTest.Language.Reading;

/// <summary>Outcome of reading one suite document.</summary>
/// <param name="Document">The bound document, or null when binding failed.</param>
/// <param name="Diagnostics">All findings, in document order.</param>
public sealed record SuiteReadResult(
    JTestSuiteDocument? Document,
    IReadOnlyList<LanguageDiagnostic> Diagnostics)
{
    /// <summary>Whether the document is valid and safe to execute.</summary>
    public bool IsValid =>
        Document is not null &&
        Diagnostics.All(static d => d.Severity != DiagnosticSeverity.Error);
}
