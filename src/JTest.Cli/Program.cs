using JTest.Cli.Core;
using JetBrains.Annotations;

namespace JTest.Cli;

[UsedImplicitly]
internal class Program
{
    /// <summary>
    /// Main entry point that delegates to the application orchestrator.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code: 0 for success, non-zero for failure</returns>
    private static async Task<int> Main(string[] args)
    {
        return await JTestApplication.RunAsync(args);
    }
}
