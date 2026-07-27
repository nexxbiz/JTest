using JTest.Cli.Invocation;
using JTest.Cli.Ports;

namespace JTest.Cli.Commands;

/// <summary>
/// Routes parsed invocations to command implementations. Unexpected
/// exceptions become exit code 3 with an honest message — never success.
/// </summary>
public sealed class CliCommandRouter
{
    private readonly RunCliCommand run;
    private readonly ValidateCliCommand validate;
    private readonly DescribeCliCommand describe;
    private readonly IConsoleWriter console;

    /// <summary>Creates the router.</summary>
    /// <param name="run">Run command.</param>
    /// <param name="validate">Validate command.</param>
    /// <param name="describe">Describe command.</param>
    /// <param name="console">Console output.</param>
    public CliCommandRouter(
        RunCliCommand run,
        ValidateCliCommand validate,
        DescribeCliCommand describe,
        IConsoleWriter console)
    {
        this.run = run;
        this.validate = validate;
        this.describe = describe;
        this.console = console;
    }

    /// <summary>Executes the invocation and returns the process exit code.</summary>
    /// <param name="invocation">The parsed invocation.</param>
    /// <param name="environment">Ambient session facts.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public async Task<int> Execute(
        CliInvocation invocation,
        CliEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            return invocation.Command switch
            {
                "run" => await run.Execute(invocation, environment, cancellationToken).ConfigureAwait(false),
                "validate" => validate.Execute(invocation, environment),
                "describe" => describe.Execute(invocation, environment),
                _ => UnknownCommand(invocation.Command),
            };
        }
        catch (OperationCanceledException)
        {
            console.ErrorLine("The run was cancelled before evidence could be written.");
            return CliExitCodes.TestsFailed;
        }
        catch (Exception exception)
        {
            console.ErrorLine($"jtest failed unexpectedly: {exception.Message}");
            return CliExitCodes.InternalError;
        }
    }

    private int UnknownCommand(string command)
    {
        console.ErrorLine($"Unknown command '{command}'.");
        return CliExitCodes.InvalidInput;
    }
}
