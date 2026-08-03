namespace JTest.Core.Tracing;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}

/// <summary>An error/warning attachable to any trace node, with an optional source location.</summary>
public sealed record Diagnostic
{
    public required DiagnosticSeverity Severity { get; init; }
    public required string Message { get; init; }

    /// <summary>JSON Pointer or file:line into the source definition (FR-031).</summary>
    public string? Location { get; init; }

    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }

    public static Diagnostic Error(string message, string? location = null) =>
        new() { Severity = DiagnosticSeverity.Error, Message = message, Location = location };

    public static Diagnostic FromException(Exception ex, string? location = null) => new()
    {
        Severity = DiagnosticSeverity.Error,
        Message = ex.Message,
        Location = location,
        ExceptionType = ex.GetType().FullName,
        StackTrace = ex.StackTrace
    };
}
