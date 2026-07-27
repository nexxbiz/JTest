using System.Diagnostics;
using System.Text.Json.Nodes;

namespace JTest.AcceptanceTests.TestSupport;

internal sealed record CliRunResult(int ExitCode, string Output, string Error, string Workspace)
{
    internal JsonObject ReadEvidence()
    {
        var line = Output.Split('\n').First(static l => l.StartsWith("Evidence: ", StringComparison.Ordinal));
        var path = line["Evidence: ".Length..].Trim();
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }
}

internal static class CliWorkspace
{
    internal static string RepoRoot { get; } = FindRoot();

    internal static string Create()
    {
        var path = Path.Combine(Path.GetTempPath(), "jtest-acceptance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static async Task<CliRunResult> RunCli(
        string workspace,
        IReadOnlyDictionary<string, string>? extraEnvironment = null,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(Path.Combine(
            RepoRoot, "src", "JTest.Cli.Host", "bin", "Release", "net10.0", "GeneratedHost.dll"));
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["CI"] = "true";
        foreach (var entry in extraEnvironment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[entry.Key] = entry.Value;
        }

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new CliRunResult(process.ExitCode, output, error, workspace);
    }

    internal static JsonObject FindNode(JsonObject trace, string path)
    {
        if (trace["path"]!.GetValue<string>() == path)
        {
            return trace;
        }

        foreach (var child in trace["children"]?.AsArray() ?? [])
        {
            var childPath = child!["path"]!.GetValue<string>();
            if (path == childPath || path.StartsWith(childPath + "/", StringComparison.Ordinal))
            {
                return FindNode(child.AsObject(), path);
            }
        }

        throw new InvalidOperationException($"Trace node '{path}' was not found.");
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "JTest.sln")))
        {
            current = current.Parent!;
        }

        return current!.FullName;
    }
}
