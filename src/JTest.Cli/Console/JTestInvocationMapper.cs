using System.Collections.Immutable;
using JTest.Cli.Invocation;

namespace JTest.Cli.Console;

/// <summary>
/// Maps typed console requests onto the parsed-invocation shape the command
/// classes consume. The mapping is mechanical: every request property lands
/// under its canonical option name, and flags map to presence.
/// </summary>
internal static class JTestInvocationMapper
{
    /// <summary>Maps the run request.</summary>
    /// <param name="request">The typed request.</param>
    internal static CliInvocation Map(JTestRunRequest request)
    {
        var options = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        AddValue(options, "env-file", request.EnvFile);
        AddValues(options, "env", request.Env);
        AddValue(options, "globals-file", request.GlobalsFile);
        AddValues(options, "secret-env", request.SecretEnv);
        AddValue(options, "report", request.Report);
        AddValue(options, "report-dir", request.ReportDir);
        AddValue(options, "report-out", request.ReportOut);
        AddValue(options, "parallel", request.Parallel);
        AddValue(options, "timeout", request.Timeout);
        AddFlag(options, "open", request.Open);
        AddFlag(options, "no-open", request.NoOpen);
        AddValue(options, "diagnostics", request.Diagnostics);
        return new CliInvocation("run", options, request.Patterns);
    }

    /// <summary>Maps the validate request.</summary>
    /// <param name="request">The typed request.</param>
    internal static CliInvocation Map(JTestValidateRequest request)
    {
        var options = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        AddValue(options, "diagnostics", request.Diagnostics);
        return new CliInvocation("validate", options, request.Patterns);
    }

    /// <summary>Maps the describe request.</summary>
    /// <param name="request">The typed request.</param>
    internal static CliInvocation Map(JTestDescribeRequest request)
    {
        var options = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        AddValue(options, "schema", request.Schema);
        AddValue(options, "output", request.Output);
        return new CliInvocation("describe", options, []);
    }

    private static void AddValue(
        Dictionary<string, IReadOnlyList<string>> options,
        string name,
        string? value)
    {
        if (value is not null)
        {
            options[name] = [value];
        }
    }

    private static void AddValues(
        Dictionary<string, IReadOnlyList<string>> options,
        string name,
        ImmutableArray<string> values)
    {
        if (!values.IsDefaultOrEmpty)
        {
            options[name] = values;
        }
    }

    private static void AddFlag(
        Dictionary<string, IReadOnlyList<string>> options,
        string name,
        bool present)
    {
        if (present)
        {
            options[name] = [];
        }
    }
}
