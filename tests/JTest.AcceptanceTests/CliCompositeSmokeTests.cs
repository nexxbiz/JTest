using System.Diagnostics;

namespace JTest.AcceptanceTests;

/// <summary>
/// Interim acceptance smoke until JT2-W080: the shipped CLI binary runs a
/// composite suite (loop + assertions) and reports truthfully.
/// </summary>
[TestClass]
public sealed class CliCompositeSmokeTests
{
    [TestMethod]
    public async Task CompositeSuitePassesThroughTheRealCli()
    {
        var root = FindRoot();
        var workspace = Path.Combine(Path.GetTempPath(), "jtest-acceptance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        File.WriteAllText(
            Path.Combine(workspace, "composite.suite.json"),
            """
            {
              "jtest": "2.0",
              "globals": { "total": 0 },
              "tests": [
                {
                  "name": "loop math",
                  "steps": [
                    {
                      "type": "for",
                      "items": [ 1, 2, 3 ],
                      "steps": [
                        { "type": "assert", "assert": [ { "op": "greaterThan", "actual": "{{$.item}}", "expected": 0 } ] }
                      ]
                    },
                    { "type": "wait", "ms": 0, "save": { "$.globals.total": "{{$.this.completedIterations}}" } },
                    { "type": "assert", "assert": [ { "op": "equals", "actual": "{{$.globals.total}}", "expected": 3 } ] }
                  ]
                }
              ]
            }
            """);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(Path.Combine(
            root, "src", "JTest.Cli.Host", "bin", "Release", "net10.0", "GeneratedHost.dll"));
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("composite.suite.json");
        startInfo.Environment["CI"] = "true";

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.AreEqual(0, process.ExitCode, output + error);
        StringAssert.Contains(output, "passed");
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
