using JTest.Cli.Settings;
using JTest.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public sealed class ValidateCommand(IAnsiConsole ansiConsole, IJTestSuiteValidator validator) : CommandBase<ValidateCommandSettings>(ansiConsole)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ValidateCommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await validator.ValidateJTestSuites(settings.TestFilePatterns!, settings.GetCategories());
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteException(ex, ExceptionFormats.NoStackTrace);
            return 1;
        }
    }
}
