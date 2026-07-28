using JTest.Cli.Commands;
using JTest.Engine.Ports;

namespace JTest.Cli.Console;

/// <summary>
/// The real session adapter: working directory and console redirection are
/// read ambiently, and the <c>CI</c> value is routed through the
/// <see cref="IProcessEnvironment"/> port so tests and hosts can substitute it.
/// </summary>
public sealed class SystemJTestCliSession : IJTestCliSession
{
    private readonly IProcessEnvironment environment;

    /// <summary>Creates the session adapter.</summary>
    /// <param name="environment">Process environment port.</param>
    public SystemJTestCliSession(IProcessEnvironment environment)
    {
        this.environment = environment;
    }

    /// <inheritdoc />
    public CliEnvironment Capture() =>
        new(
            Directory.GetCurrentDirectory(),
            environment.GetValue("CI"),
            global::System.Console.IsOutputRedirected);
}
