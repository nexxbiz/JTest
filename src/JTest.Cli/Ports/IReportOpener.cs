namespace JTest.Cli.Ports;

/// <summary>Best-effort opener for the report page.</summary>
public interface IReportOpener
{
    /// <summary>Attempts to open the URL with the platform default handler.</summary>
    /// <param name="url">The report URL.</param>
    /// <returns>Whether the attempt was accepted by the platform.</returns>
    bool TryOpen(string url);
}
