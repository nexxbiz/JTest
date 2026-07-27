namespace JTest.Cli.Invocation;

/// <summary>
/// One parsed command invocation, decoupled from the generated host's parse
/// result so command logic stays independently testable.
/// </summary>
/// <param name="Command">The space-joined command path, e.g. <c>run</c>.</param>
/// <param name="Options">Canonical option name to its occurrence values.</param>
/// <param name="Arguments">Positional argument tokens.</param>
public sealed record CliInvocation(
    string Command,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Options,
    IReadOnlyList<string> Arguments)
{
    /// <summary>Returns the last value of an option, or null.</summary>
    /// <param name="name">Canonical option name.</param>
    public string? LastValue(string name) =>
        Options.TryGetValue(name, out var values) && values.Count > 0 ? values[^1] : null;

    /// <summary>Returns every value of an option.</summary>
    /// <param name="name">Canonical option name.</param>
    public IReadOnlyList<string> Values(string name) =>
        Options.TryGetValue(name, out var values) ? values : [];

    /// <summary>Returns whether a flag option occurred.</summary>
    /// <param name="name">Canonical option name.</param>
    public bool HasFlag(string name) => Options.ContainsKey(name);
}
