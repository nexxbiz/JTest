using JTest.Language.Diagnostics;

namespace JTest.Language.Reading;

/// <summary>Static shorthand for appending diagnostics during binding.</summary>
internal static class Diag
{
    internal static void Error(
        ICollection<LanguageDiagnostic> sink,
        string code,
        string message,
        string source,
        string jsonPointer,
        string? hint = null) =>
        sink.Add(new LanguageDiagnostic(code, DiagnosticSeverity.Error, message, source, jsonPointer, hint));

    internal static void Warning(
        ICollection<LanguageDiagnostic> sink,
        string code,
        string message,
        string source,
        string jsonPointer,
        string? hint = null) =>
        sink.Add(new LanguageDiagnostic(code, DiagnosticSeverity.Warning, message, source, jsonPointer, hint));
}
