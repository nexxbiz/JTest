namespace JTest.Cli.Commands;

/// <summary>
/// The decided auto-open behavior: the report URL is always printed; opening
/// is best-effort, defaults to interactive sessions only, and never changes
/// the exit code.
/// </summary>
public static class OpenBehavior
{
    /// <summary>Decides whether to attempt opening the report.</summary>
    /// <param name="openRequested">An explicit <c>--open</c>.</param>
    /// <param name="noOpenRequested">An explicit <c>--no-open</c>.</param>
    /// <param name="environment">Ambient session facts.</param>
    public static bool ShouldOpen(bool openRequested, bool noOpenRequested, CliEnvironment environment)
    {
        if (noOpenRequested)
        {
            return false;
        }

        if (openRequested)
        {
            return true;
        }

        var inContinuousIntegration =
            !string.IsNullOrEmpty(environment.CiValue) &&
            !string.Equals(environment.CiValue, "false", StringComparison.OrdinalIgnoreCase);

        return !inContinuousIntegration && !environment.OutputRedirected;
    }
}
