namespace JTest.Cli.Ports;

/// <summary>The real console.</summary>
public sealed class SystemConsoleWriter : IConsoleWriter
{
    /// <inheritdoc />
    public void Out(string line) => Console.Out.WriteLine(line);

    /// <inheritdoc />
    public void ErrorLine(string line) => Console.Error.WriteLine(line);
}
