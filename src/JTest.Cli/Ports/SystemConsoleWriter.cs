namespace JTest.Cli.Ports;

/// <summary>The real console.</summary>
public sealed class SystemConsoleWriter : IConsoleWriter
{
    /// <inheritdoc />
    public void Out(string line) => global::System.Console.Out.WriteLine(line);

    /// <inheritdoc />
    public void ErrorLine(string line) => global::System.Console.Error.WriteLine(line);
}
