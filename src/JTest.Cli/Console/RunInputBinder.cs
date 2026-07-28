using System.Globalization;
using System.Text.Json;
using JTest.Cli.Commands;
using JTest.Cli.Invocation;
using JTest.Engine.Execution;

namespace JTest.Cli.Console;

/// <summary>
/// Binds and pre-checks the run inputs: environment pairs and value files,
/// globals, secret keys, and parallelism. Moved unchanged from the run
/// command so the host-side validator and the command share one truth.
/// </summary>
public static class RunInputBinder
{
    /// <summary>Builds the run options, or reports the exact input error.</summary>
    /// <param name="invocation">The parsed invocation.</param>
    /// <param name="environment">Ambient session facts.</param>
    /// <param name="options">The bound options on success.</param>
    /// <param name="error">The input error on failure.</param>
    public static bool TryBind(
        CliInvocation invocation,
        CliEnvironment environment,
        out RunOptions options,
        out string error)
    {
        options = new RunOptions();
        error = string.Empty;

        var env = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryReadValueFile(invocation.LastValue("env-file"), environment, env, ref error))
        {
            return false;
        }

        foreach (var pair in invocation.Values("env"))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                error = $"Invalid --env value '{pair}'; expected key=value.";
                return false;
            }

            var key = pair[..separator];
            if (!env.TryAdd(key, pair[(separator + 1)..]))
            {
                error = $"Duplicate --env key '{key}'.";
                return false;
            }
        }

        var globals = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryReadValueFile(invocation.LastValue("globals-file"), environment, globals, ref error))
        {
            return false;
        }

        var parallelism = 1;
        var parallelText = invocation.LastValue("parallel");
        if (parallelText is not null &&
            (!int.TryParse(parallelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out parallelism) ||
             parallelism < 1))
        {
            error = $"Invalid --parallel value '{parallelText}'; expected a positive integer.";
            return false;
        }

        options = new RunOptions(
            parallelism,
            env,
            globals,
            invocation.Values("secret-env"));
        return true;
    }

    private static bool TryReadValueFile(
        string? relativePath,
        CliEnvironment environment,
        Dictionary<string, string> target,
        ref string error)
    {
        if (relativePath is null)
        {
            return true;
        }

        var path = Path.GetFullPath(Path.Combine(environment.WorkingDirectory, relativePath));
        if (!File.Exists(path))
        {
            error = $"Value file '{relativePath}' does not exist.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var property in document.RootElement.EnumerateObject())
            {
                target[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()!
                    : property.Value.GetRawText();
            }

            return true;
        }
        catch (JsonException exception)
        {
            error = $"Value file '{relativePath}' is not a JSON object: {exception.Message}";
            return false;
        }
    }
}
