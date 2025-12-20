using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public abstract class CommandBase<TSettings>(IAnsiConsole ansiConsole) : ICommand<TSettings>
    where TSettings : CommandSettings
{
    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);

    protected IAnsiConsole Console => ansiConsole;

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
                AnsiConsole.WriteException(
                    e,
                    ExceptionFormats.NoStackTrace
                );

                Environment.ExitCode = -1;
                return -1;
            }
        }

        var exception = new InvalidProgramException(
            $"Command settings were not expected type '{typeof(TSettings).FullName}'"
        );

        ansiConsole.WriteException(
            exception,
            ExceptionFormats.NoStackTrace
        );

        Environment.ExitCode = -1;
        return -1;
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        return settings.Validate();
    }
}
