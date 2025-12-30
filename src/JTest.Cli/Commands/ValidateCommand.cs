using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public sealed class ValidateCommand(
    IAnsiConsole ansiConsole,
    IErrorHandlingService errorHandlingService,
    IJTestSuiteValidator validator)
    : CommandBase<ValidateCommandSettings>(ansiConsole, errorHandlingService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ValidateCommandSettings settings, CancellationToken cancellationToken)
    {
        await validator.ValidateJTestSuites(settings.TestFilePatterns!, settings.GetCategories());
        return 0;
    }
}
