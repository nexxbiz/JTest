using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using JetBrains.Annotations;

namespace JTest.Cli.Settings;

[UsedImplicitly]
public sealed class CreateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("Name of the test file")]
    public string? Name { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Name) 
            ? ValidationResult.Error("Name argument must be specified") 
            : ValidationResult.Success();
    }
}
