using JTest.Cli.Commands;

namespace JTest.Cli.Console;

/// <summary>Supplies the ambient session facts command execution needs.</summary>
public interface IJTestCliSession
{
    /// <summary>Captures the current session facts.</summary>
    CliEnvironment Capture();
}
