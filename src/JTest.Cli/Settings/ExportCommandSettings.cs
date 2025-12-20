using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace JTest.Cli.Settings;

public sealed class ExportCommandSettings : CommandSettings
{
    [CommandArgument(0, "<format>")]
    [Description("Format of the export. Supported formats are: 'postman, karate'")]
    public string? Format { get; set; }

    [CommandArgument(1, "<test-file>")]
    [Description("Path to test file that will be exported")]
    public string? TestFilePath { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output folder path where reports are saved (default: working directory)")]
    public string? OutputDirectoryPath { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Format))
        {
            return ValidationResult.Error("Format argument must be specified");
        }
        if (string.IsNullOrWhiteSpace(TestFilePath))
        {
            return ValidationResult.Error("Format argument must be specified");
        }
        if (!File.Exists(TestFilePath))
        {
            return ValidationResult.Error($"Test file at path '{TestFilePath}' does not exist");
        }

        return ValidationResult.Success();
    }
}
