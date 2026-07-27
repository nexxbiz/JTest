namespace JTest.Cli.Hosting;

/// <summary>
/// Placeholder entry point. The real host is generated from the typed Open
/// Console document in work unit JT2-W070; until then every invocation
/// reports honestly that the CLI is not implemented and exits with the
/// internal-failure code so no pipeline can mistake it for success.
/// </summary>
internal static class Program
{
    internal static int Main()
    {
        Console.Error.WriteLine(
            "jtest 2.0.0-alpha.1: command-line host not yet implemented (JT2-W070).");
        return 3;
    }
}
