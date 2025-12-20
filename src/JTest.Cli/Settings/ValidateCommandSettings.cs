using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace JTest.Cli.Settings;

public sealed class ValidateCommandSettings : CommandSettings
{
    private string[] categories = [];

    [CommandArgument(0, "<test-file|pattern>")]
    [Description("Paths or patterns to test files")]
    public IEnumerable<string>? TestFilePatterns { get; set; }

    [CommandOption("-c|--categories")]
    [Description("Comma-separated list of test file categories to run (default: all)")]
    public string? Categories { get; set; }

    public IEnumerable<string> GetCategories()
    {
        return categories;
    }

    public override ValidationResult Validate()
    {
        if (TestFilePatterns?.Any() != true)
        {
            return ValidationResult.Error("Test files/patterns argument must be specified");
        }

        if (!string.IsNullOrWhiteSpace(Categories))
        {
            categories = Categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return ValidationResult.Success();
    }
}
