using JTest.Cli.Tests.TestSupport;
using JTest.Language;

namespace JTest.Cli.Tests.Host;

/// <summary>
/// End-to-end proof of the frozen jtest exit-code contract against the
/// generated console host binary — the exact process a pipeline runs.
/// </summary>
[TestClass]
public sealed class GeneratedHostExitCodeTests
{
    private static string workspace = string.Empty;

    [ClassInitialize]
    public static void CreateWorkspace(TestContext context)
    {
        _ = context;
        workspace = Path.Combine(Path.GetTempPath(), "jtest-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        File.WriteAllText(
            Path.Combine(workspace, "pass.suite.json"),
            """
            { "jtest": "2.0", "tests": [ { "name": "ok", "steps": [
              { "type": "assert", "assert": [ { "op": "equals", "actual": 1, "expected": 1 } ] } ] } ] }
            """);
        File.WriteAllText(
            Path.Combine(workspace, "fail.suite.json"),
            """
            { "jtest": "2.0", "tests": [ { "name": "bad", "steps": [
              { "type": "assert", "assert": [ { "op": "equals", "actual": 1, "expected": 2 } ] },
              { "type": "wait", "ms": 0 } ] } ] }
            """);
        File.WriteAllText(
            Path.Combine(workspace, "invalid.suite.json"),
            """{ "jtest": "2.0", "tests": [ { "name": "broken", "steps": [ { "type": "sql" } ] } ] }""");
    }

    [TestMethod]
    public async Task PassingSuiteExitsZeroAndPrintsReportUrl()
    {
        var result = await CliProcess.Run(workspace, "run", "pass.suite.json");

        Assert.AreEqual(0, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Output, "Report: file:///");
        StringAssert.Contains(result.Output, "Evidence: ");
        Assert.IsTrue(File.Exists(Path.Combine(workspace, ".jtest", "reports", "index.html")));
        Assert.IsTrue(File.Exists(Path.Combine(workspace, ".jtest", "reports", "catalog.js")));
    }

    [TestMethod]
    public async Task FailingSuiteExitsOneWithEvidence()
    {
        var result = await CliProcess.Run(workspace, "run", "fail.suite.json");

        Assert.AreEqual(1, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Output, "failed");
        StringAssert.Contains(result.Output, "Report: file:///");
    }

    [TestMethod]
    public async Task InvalidSuiteExitsTwoWithoutExecuting()
    {
        var result = await CliProcess.Run(workspace, "run", "invalid.suite.json", "--diagnostics=json");

        Assert.AreEqual(2, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Output, "JT0104");
    }

    [TestMethod]
    public async Task NoMatchingFilesExitsTwo()
    {
        var result = await CliProcess.Run(workspace, "run", "nothing-here-*.json");
        Assert.AreEqual(2, result.ExitCode, result.Output + result.Error);
    }

    [TestMethod]
    public async Task UnknownOptionExitsTwoFromTheGeneratedParser()
    {
        var result = await CliProcess.Run(workspace, "run", "pass.suite.json", "--bogus");
        Assert.AreEqual(2, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Error + result.Output, "PKNETC002");
    }

    [TestMethod]
    public async Task StandaloneReportModeWritesSelfContainedArtifact()
    {
        var result = await CliProcess.Run(
            workspace, "run", "pass.suite.json", "--report=standalone", "--report-out=artifact");

        Assert.AreEqual(0, result.ExitCode, result.Output + result.Error);
        var html = File.ReadAllText(Path.Combine(workspace, "artifact", "index.html"));
        StringAssert.Contains(html, "window.__JTEST_RUN__");
        Assert.IsTrue(File.Exists(Path.Combine(workspace, "artifact", "result.json")));
    }

    [TestMethod]
    public async Task DescribeEmitsTheExactEmbeddedManifest()
    {
        var result = await CliProcess.Run(workspace, "describe");

        Assert.AreEqual(0, result.ExitCode, result.Error);
        Assert.AreEqual(
            LanguageContract.LanguageManifestJson.TrimEnd('\n', '\r'),
            result.Output.TrimEnd('\n', '\r').Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ValidateReportsJsonDiagnosticsAndExitsTwo()
    {
        var result = await CliProcess.Run(workspace, "validate", "invalid.suite.json", "--diagnostics=json");

        Assert.AreEqual(2, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Output, "\"code\":\"JT0104\"");
    }

    [TestMethod]
    public async Task HelpExitsZeroAndListsCommands()
    {
        var result = await CliProcess.Run(workspace, "--help");

        Assert.AreEqual(0, result.ExitCode, result.Error);
        StringAssert.Contains(result.Output, "run");
        StringAssert.Contains(result.Output, "validate");
        StringAssert.Contains(result.Output, "describe");
    }
}
