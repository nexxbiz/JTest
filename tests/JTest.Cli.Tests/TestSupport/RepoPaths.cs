namespace JTest.Cli.Tests.TestSupport;

internal static class RepoPaths
{
    internal static string Root { get; } = FindRoot();

    internal static string HostAssembly { get; } = Path.Combine(
        FindRoot(), "src", "JTest.Cli.Host", "bin", "Release", "net10.0", "GeneratedHost.dll");

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "JTest.sln")))
        {
            current = current.Parent!;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("JTest.sln was not found above the test directory.");
    }
}
