namespace JTest.Language.Diagnostics;

/// <summary>
/// One machine-readable validation finding. Codes are stable and append-only
/// (see docs/language/diagnostics.md); pointers are RFC 6901 JSON pointers
/// into the offending document.
/// </summary>
/// <param name="Code">Stable diagnostic code, e.g. <c>JT0101</c>.</param>
/// <param name="Severity">Whether the finding blocks execution.</param>
/// <param name="Message">Human-readable, single-sentence description.</param>
/// <param name="Source">Name or path of the offending document.</param>
/// <param name="JsonPointer">JSON pointer to the offending location.</param>
/// <param name="Hint">Optional remediation hint.</param>
public sealed record LanguageDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    string Source,
    string JsonPointer,
    string? Hint = null);
