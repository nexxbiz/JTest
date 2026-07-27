using System.Diagnostics;

namespace JTest.Cli.Ports;

/// <summary>Opens URLs through the platform shell.</summary>
public sealed class ProcessStartReportOpener : IReportOpener
{
    /// <inheritdoc />
    public bool TryOpen(string url)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return process is not null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
                                          or InvalidOperationException
                                          or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
