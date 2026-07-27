using JTest.Cli.Ports;

namespace JTest.Cli.Tests.TestSupport;

internal sealed class RecordingConsole : IConsoleWriter
{
    internal List<string> OutLines { get; } = [];

    internal List<string> ErrorLines { get; } = [];

    public void Out(string line) => OutLines.Add(line);

    public void ErrorLine(string line) => ErrorLines.Add(line);
}
