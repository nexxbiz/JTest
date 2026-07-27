using System.Diagnostics;

namespace JTest.Cli.Tests.TestSupport;

internal sealed record CliProcessResult(int ExitCode, string Output, string Error);

internal static class CliProcess
{
    internal static async Task<CliProcessResult> Run(
        string workingDirectory,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(RepoPaths.HostAssembly);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // The tests must never auto-open a browser.
        startInfo.Environment["CI"] = "true";

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new CliProcessResult(process.ExitCode, output, error);
    }
}
