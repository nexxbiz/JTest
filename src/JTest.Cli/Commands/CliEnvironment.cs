namespace JTest.Cli.Commands;

/// <summary>Ambient facts the host supplies to command execution.</summary>
/// <param name="WorkingDirectory">Directory patterns and defaults resolve against.</param>
/// <param name="CiValue">Value of the <c>CI</c> environment variable, if any.</param>
/// <param name="OutputRedirected">Whether standard output is redirected.</param>
public sealed record CliEnvironment(
    string WorkingDirectory,
    string? CiValue,
    bool OutputRedirected);
