using System.Collections.Immutable;

namespace JTest.Cli.Console;

/// <summary>
/// The typed <c>jtest run</c> request. Constructor parameters mirror the
/// Open Console grammar exactly: one parameter per argument and option, in
/// binding order, so the generated host can construct the request without
/// interpretation.
/// </summary>
public sealed class JTestRunRequest
{
    /// <summary>Creates the request.</summary>
    /// <param name="patterns">Suite file paths or glob patterns.</param>
    /// <param name="envFile">JSON file merged into the case environment, or null.</param>
    /// <param name="env">Inline <c>key=value</c> environment overrides.</param>
    /// <param name="globalsFile">JSON file providing run globals, or null.</param>
    /// <param name="secretEnv">Environment keys whose values are secret.</param>
    /// <param name="report">Report mode: <c>catalog</c> or <c>standalone</c>.</param>
    /// <param name="reportDir">Catalog report directory override, or null.</param>
    /// <param name="reportOut">Standalone report directory override, or null.</param>
    /// <param name="parallel">Maximum suite parallelism as decimal text.</param>
    /// <param name="timeout">Run timeout in milliseconds as decimal text, or null.</param>
    /// <param name="open">Whether opening the report was explicitly requested.</param>
    /// <param name="noOpen">Whether opening the report was explicitly suppressed.</param>
    /// <param name="diagnostics">Diagnostics format: <c>text</c> or <c>json</c>.</param>
    public JTestRunRequest(
        ImmutableArray<string> patterns,
        string? envFile,
        ImmutableArray<string> env,
        string? globalsFile,
        ImmutableArray<string> secretEnv,
        string report,
        string? reportDir,
        string? reportOut,
        string parallel,
        string? timeout,
        bool open,
        bool noOpen,
        string diagnostics)
    {
        Patterns = patterns;
        EnvFile = envFile;
        Env = env;
        GlobalsFile = globalsFile;
        SecretEnv = secretEnv;
        Report = report;
        ReportDir = reportDir;
        ReportOut = reportOut;
        Parallel = parallel;
        Timeout = timeout;
        Open = open;
        NoOpen = noOpen;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the suite file paths or glob patterns.</summary>
    public ImmutableArray<string> Patterns { get; }

    /// <summary>Gets the environment value file path, or null.</summary>
    public string? EnvFile { get; }

    /// <summary>Gets the inline environment overrides.</summary>
    public ImmutableArray<string> Env { get; }

    /// <summary>Gets the globals value file path, or null.</summary>
    public string? GlobalsFile { get; }

    /// <summary>Gets the secret environment keys.</summary>
    public ImmutableArray<string> SecretEnv { get; }

    /// <summary>Gets the report mode.</summary>
    public string Report { get; }

    /// <summary>Gets the catalog report directory override, or null.</summary>
    public string? ReportDir { get; }

    /// <summary>Gets the standalone report directory override, or null.</summary>
    public string? ReportOut { get; }

    /// <summary>Gets the maximum parallelism text.</summary>
    public string Parallel { get; }

    /// <summary>Gets the run timeout text, or null.</summary>
    public string? Timeout { get; }

    /// <summary>Gets whether opening the report was explicitly requested.</summary>
    public bool Open { get; }

    /// <summary>Gets whether opening the report was explicitly suppressed.</summary>
    public bool NoOpen { get; }

    /// <summary>Gets the diagnostics format.</summary>
    public string Diagnostics { get; }
}
