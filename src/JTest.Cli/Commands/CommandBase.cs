using JTest.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public abstract class CommandBase<TSettings>(IAnsiConsole ansiConsole, IErrorHandlingService errorHandlingService)
    : ICommand<TSettings>
    where TSettings : CommandSettings
{
    protected IAnsiConsole Console { get; } = ansiConsole;
    private IErrorHandlingService ErrorHandler { get; } = errorHandlingService;

    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);

    public async Task<int> ExecuteAsync(CommandContext context, CommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings is TSettings typedSettings)
        {
            try
            {
                var result = await ExecuteAsync(context, typedSettings, cancellationToken);
                Environment.ExitCode = result;
                return result;
            }
            catch (Exception e)
            {
                return ErrorHandler.HandleException(e, $"command '{context.Name}'");
            }
        }

        var exception = new InvalidProgramException(
            $"Command settings were not expected type '{typeof(TSettings).FullName}'"
        );

        return ErrorHandler.HandleException(exception, "command initialization");
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        return settings.Validate();
    }
}
