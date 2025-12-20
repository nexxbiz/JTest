using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace JTest.Cli.Settings;

public sealed class CreateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("Name of the test file")]
    public string? Name { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Name argument must be specified");
        }

        return ValidationResult.Success();
    }
}
