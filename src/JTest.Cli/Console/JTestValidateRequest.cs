using System.Collections.Immutable;

namespace JTest.Cli.Console;

/// <summary>The typed <c>jtest validate</c> request.</summary>
public sealed class JTestValidateRequest
{
    /// <summary>Creates the request.</summary>
    /// <param name="patterns">Suite file paths or glob patterns.</param>
    /// <param name="diagnostics">Diagnostics format: <c>text</c> or <c>json</c>.</param>
    public JTestValidateRequest(
        ImmutableArray<string> patterns,
        string diagnostics)
    {
        Patterns = patterns;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the suite file paths or glob patterns.</summary>
    public ImmutableArray<string> Patterns { get; }

    /// <summary>Gets the diagnostics format.</summary>
    public string Diagnostics { get; }
}
