using JTest.Language.Diagnostics;
using JTest.Language.Semantics;

namespace JTest.Cli.Loading;

/// <summary>One discovered suite file after reading and validation.</summary>
/// <param name="FilePath">Absolute suite file path.</param>
/// <param name="Bundle">The validated bundle; null when invalid.</param>
/// <param name="Diagnostics">Every finding from reading and validation.</param>
public sealed record LoadedSuite(
    string FilePath,
    SuiteBundle? Bundle,
    IReadOnlyList<LanguageDiagnostic> Diagnostics)
{
    /// <summary>Whether the suite is safe to execute.</summary>
    public bool IsValid =>
        Bundle is not null &&
        Diagnostics.All(static d => d.Severity != DiagnosticSeverity.Error);
}
