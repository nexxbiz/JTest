using JTest.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;

namespace JTest.Cli.Settings;

[UsedImplicitly]
public sealed class RunCommandSettings : CommandSettings
{
    private IEnumerable<string>? categories;
    private IReadOnlyDictionary<string, object?>? environmentVariables;
    private IReadOnlyDictionary<string, object?>? globalVariables;

    [CommandArgument(0, "<test-file-patterns>")]
    [Description("List of test file patterns that are executed in order. Example: \"tests/**/*\" \"!tests/obsolete-tests/*\"")]
    public string[] TestFilePatterns { get; set; } = [];

    [CommandOption("--env-file")]
    [Description("File path to environment variables")]
    public string? EnvironmentVariablesFile { get; set; }

    // Must be an array: Spectre.Console.Cli only binds repeated occurrences of an option to an
    // array property, so an IEnumerable<string> stayed null and every -e value was dropped (#74).
    [CommandOption("-e|--env")]
    [Description("Environment variable formatted as key=value. You can specify the option multiple times to define multiple environment variables. Takes precedence over --env-file")]
    public string[]? EnvironmentVariables { get; set; }

    [CommandOption("--globals-file")]
    [Description("File path to global variables")]
    public string? GlobalVariablesFile { get; set; }

    [CommandOption("-p|--parallel")]
    [Description("Run test files in parallel (default: 1)")]
    public int? ParallelTestExecutionCount { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output folder for the report and trace when no explicit --report/--trace path is given (default: artifacts)")]
    public string? OutputDirectoryPath { get; set; }

    [CommandOption("-c|--categories")]
    [Description("Comma-separated list of test file categories to run (default: all)")]
    public string? Categories { get; set; }

    [CommandOption("--skip-output")]
    [Description("Do not write the default report/trace files (explicit --report/--trace are still written). Default: false")]
    public bool? SkipOutput { get; set; }

    [CommandOption("-f|--output-format")]
    [Description("Default report format when --report is not given: 'html' (default) or 'markdown'")]
    public string? OutputFormat { get; set; }

    [CommandOption("--report")]
    [Description("Write a report to the given file path (see --report-format).")]
    public string? ReportFile { get; set; }

    [CommandOption("--report-format")]
    [Description("Format for --report: 'html' (default, self-contained) or 'markdown'.")]
    public string? ReportFormat { get; set; }

    [CommandOption("--trace")]
    [Description("Write the canonical execution-trace JSON to the given file path.")]
    public string? TraceFile { get; set; }

    [CommandOption("--include-variables")]
    [Description("Include a masked dump of environment/global variables in the report and trace (off by default).")]
    public bool IncludeVariables { get; set; }

    public IEnumerable<string> GetCategories()
    {
        return categories ?? [];
    }

    public IReadOnlyDictionary<string, object?>? GetEnvironmentVariables()
    {
        return environmentVariables;
    }

    public IReadOnlyDictionary<string, object?>? GetGlobalVariables()
    {
        return globalVariables;
    }

    public override ValidationResult Validate()
    {
        if (!(TestFilePatterns?.Length > 0))
        {
            return ValidationResult.Error("At least one test file pattern is required.");
        }
        if (!string.IsNullOrWhiteSpace(EnvironmentVariablesFile) && !File.Exists(EnvironmentVariablesFile))
        {
            return ValidationResult.Error($"Environment variables file at path '{EnvironmentVariablesFile}' cannot be found.");
        }
        if (!string.IsNullOrWhiteSpace(GlobalVariablesFile) && !File.Exists(GlobalVariablesFile))
        {
            return ValidationResult.Error($"Environment variables file at path '{GlobalVariablesFile}' cannot be found.");
        }

        if (!string.IsNullOrWhiteSpace(Categories))
        {
            categories = Categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (EnvironmentVariables?.Length > 0 || !string.IsNullOrWhiteSpace(EnvironmentVariablesFile))
        {
            var environmentResult = SetEnvironmentVariables();
            if (!environmentResult.Successful)
            {
                return environmentResult;
            }
        }

        if (!string.IsNullOrWhiteSpace(GlobalVariablesFile))
        {
            globalVariables = GetVariableFromFile(GlobalVariablesFile)?.AsReadOnly();
        }

        return ValidationResult.Success();
    }

    private static Dictionary<string, object?>? GetVariableFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(
            json,
            JsonSerializerOptionsAccessor.Default
        );
    }

    /// <summary>
    /// Builds the effective environment from both sources: <c>--env-file</c> first, then
    /// <c>-e/--env</c> laid over it, so the command line — the more specific source — wins on a key
    /// given twice instead of throwing on a duplicate key.
    /// </summary>
    private ValidationResult SetEnvironmentVariables()
    {
        var merged = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(EnvironmentVariablesFile))
        {
            foreach (var env in GetVariableFromFile(EnvironmentVariablesFile) ?? [])
                merged[env.Key] = env.Value;
        }

        foreach (var entry in EnvironmentVariables ?? [])
        {
            // Only the first '=' separates key from value; values legitimately contain more of them
            // (connection strings, base64 padding), so the rest is taken verbatim.
            var separatorIndex = entry.IndexOf('=');
            var key = separatorIndex < 0 ? string.Empty : entry[..separatorIndex].Trim();
            if (key.Length == 0)
            {
                return ValidationResult.Error(
                    $"Environment variable '{entry}' must be formatted as key=value.");
            }

            merged[key] = entry[(separatorIndex + 1)..];
        }

        environmentVariables = merged.AsReadOnly();
        return ValidationResult.Success();
    }
}
