using JTest.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;

namespace JTest.Cli.Settings;

public sealed class RunCommandSettings : CommandSettings
{
    private IEnumerable<string>? categories;
    private IReadOnlyDictionary<string, object?>? environmentVariables;
    private IReadOnlyDictionary<string, object?>? globalVariables;

    [CommandArgument(0, "<test-file-patterns>")]
    [Description("List of test file patterns that are executed in order. Example: \"tests/**/*\" \"!tests/obsolete-tests/*\"")]
    public IEnumerable<string>? TestFilePatterns { get; set; }

    [CommandOption("--env-file")]
    [Description("File path to environment variables")]
    public string? EnvironmentVariablesFile { get; set; }

    [CommandOption("-e|--env")]
    [Description("Environment variable formatted as key=value. You can specify the option multiple times to define multiple enviornment variables")]
    public IEnumerable<string>? EnvironmentVariables { get; set; }

    [CommandOption("--globals-file")]
    [Description("File path to global variables")]
    public string? GlobalVariablesFile { get; set; }

    [CommandOption("-p|--parallel")]
    [Description("Run test files in parallel (default: 1)")]
    public int? ParallelTestExecutionCount { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output folder path where reports are saved (default: working directory)")]
    public string? OutputDirectoryPath { get; set; }

    [CommandOption("-c|--categories")]
    [Description("Comma-separated list of test file categories to run (default: all)")]
    public string? Categories { get; set; }

    [CommandOption("--skip-output")]
    [Description("When specified, then does not output a report file (default: false)")]
    public bool? SkipOutput { get; set; }

    [CommandOption("-f|--output-format")]
    [Description("One of the following output formats: 'markdown' (default: markdown)")]
    public string? OutputFormat { get; set; }

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
        if (TestFilePatterns?.Any() != true)
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

        if (EnvironmentVariables?.Any() == true || !string.IsNullOrWhiteSpace(EnvironmentVariablesFile))
        {
            SetEnvironmentVariables();
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

    private void SetEnvironmentVariables()
    {
        var environmentVariables = EnvironmentVariables?.ToDictionary(
            x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First(),
            x => (object?)x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()
        )
        ?? [];

        if (!string.IsNullOrWhiteSpace(EnvironmentVariablesFile))
        {
            var envVarsFromFile = GetVariableFromFile(EnvironmentVariablesFile);
            foreach (var env in envVarsFromFile ?? [])
                environmentVariables.Add(env.Key, env.Value);
        }

        this.environmentVariables = environmentVariables.AsReadOnly();
    }
}
