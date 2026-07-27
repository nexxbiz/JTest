namespace JTest.Cli.Ports;

/// <summary>Console output port.</summary>
public interface IConsoleWriter
{
    /// <summary>Writes one line to standard output.</summary>
    /// <param name="line">The line.</param>
    void Out(string line);

    /// <summary>Writes one line to standard error.</summary>
    /// <param name="line">The line.</param>
    void ErrorLine(string line);
}
