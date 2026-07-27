namespace JTest.Language.Diagnostics;

/// <summary>Severity of a language diagnostic.</summary>
public enum DiagnosticSeverity
{
    /// <summary>The document is invalid and must not be executed.</summary>
    Error,

    /// <summary>The document is valid but contains a hazard worth surfacing.</summary>
    Warning,
}
