using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core;
using JTest.Core.Execution;
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
        var summary = await validator.ValidateJTestSuites(settings.TestFilePatterns!, settings.GetCategories());

        // Any invalid file fails validation as a CI gate (FR-004).
        return summary.HasInvalid ? (int)RunExitCode.ValidationError : (int)RunExitCode.Success;
    }
}
