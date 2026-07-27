using System.Text.Json.Nodes;
using JTest.AcceptanceTests.TestSupport;

namespace JTest.AcceptanceTests;

/// <summary>
/// One acceptance proof per verified 1.x finding (F1-F8 in
/// design/jtest-2.0/findings-1.x.md), executed through the real generated
/// CLI binary against a local in-process API.
/// </summary>
[TestClass]
public sealed class FindingsAcceptanceTests
{
    private static string ExamplePath(string file) =>
        Path.Combine(CliWorkspace.RepoRoot, "examples", "orders", file);

    [TestMethod]
    public async Task F1F2F3OrdersExampleKeepsEveryIterationWithTruthfulAncestry()
    {
        await using var api = new LocalEchoApi();
        var workspace = CliWorkspace.Create();

        var result = await CliWorkspace.RunCli(
            workspace,
            null,
            "run",
            ExamplePath("orders.suite.json"),
            "--env",
            $"baseUrl={api.BaseUrl}");

        Assert.AreEqual(0, result.ExitCode, result.Output + result.Error);
        var evidence = result.ReadEvidence();
        var trace = evidence["trace"]!.AsObject();

        // F3: the template invocation and its inner steps are in the normal
        // evidence — no debug mode exists.
        var invocation = CliWorkspace.FindNode(trace, "suites/0/cases/0/datasets/0/steps/0/invocation");
        Assert.AreEqual("templateInvocation", invocation["kind"]!.GetValue<string>());
        Assert.AreEqual(1, invocation["children"]!.AsArray().Count);

        // F1: every while pass and every for iteration is preserved.
        var poll = CliWorkspace.FindNode(trace, "suites/0/cases/0/datasets/0/steps/2");
        Assert.AreEqual(3, poll["children"]!.AsArray().Count, "three poll passes");
        var loop = CliWorkspace.FindNode(trace, "suites/0/cases/0/datasets/0/steps/3");
        Assert.AreEqual(2, loop["children"]!.AsArray().Count, "two stock iterations");

        // F2: nested nodes carry exact paths, ordinals, and iteration indexes.
        var secondIteration = loop["children"]!.AsArray()[1]!.AsObject();
        Assert.AreEqual(1, secondIteration["iterationIndex"]!.GetValue<int>());
        Assert.AreEqual(2, secondIteration["ordinal"]!.GetValue<int>());
        Assert.AreEqual(
            "suites/0/cases/0/datasets/0/steps/3/iterations/1/steps/0",
            secondIteration["children"]!.AsArray()[0]!["path"]!.GetValue<string>());

        // Both dataset runs executed.
        Assert.AreEqual(2, evidence["counts"]!["caseRuns"]!["total"]!.GetValue<int>());
        Assert.AreEqual(2, evidence["counts"]!["caseRuns"]!["passed"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task F4TransportFailureCanNeverExitZero()
    {
        var workspace = CliWorkspace.Create();
        File.WriteAllText(
            Path.Combine(workspace, "crash.suite.json"),
            """
            { "jtest": "2.0", "tests": [ { "name": "unreachable", "steps": [
              { "type": "http", "method": "GET", "url": "http://127.0.0.1:9/nothing", "timeoutMs": 2000 },
              { "type": "wait", "ms": 0 } ] } ] }
            """);

        var result = await CliWorkspace.RunCli(workspace, null, "run", "crash.suite.json");

        Assert.AreEqual(1, result.ExitCode, result.Output + result.Error);
        var trace = result.ReadEvidence()["trace"]!.AsObject();
        var step = CliWorkspace.FindNode(trace, "suites/0/cases/0/steps/0");
        var outcome = step["outcome"]!.GetValue<string>();
        Assert.AreNotEqual("passed", outcome);
        var carriesExplanation = outcome == "timedOut"
            ? step["evidence"]!["timedOutAfterMs"] is not null
            : step["diagnostics"]!.AsArray().Count > 0;
        Assert.IsTrue(carriesExplanation, $"the {outcome} step carries its explanation in the evidence");
        var follower = CliWorkspace.FindNode(trace, "suites/0/cases/0/steps/1");
        Assert.AreEqual("skipped", follower["outcome"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task F5ValidationSummaryCountsHonestly()
    {
        var workspace = CliWorkspace.Create();
        File.WriteAllText(
            Path.Combine(workspace, "good.suite.json"),
            """{ "jtest": "2.0", "tests": [ { "name": "ok", "steps": [ { "type": "wait", "ms": 0 } ] } ] }""");
        File.WriteAllText(
            Path.Combine(workspace, "bad.suite.json"),
            """{ "jtest": "2.0", "tests": [ { "name": "broken", "steps": [ { "type": "sql" } ] } ] }""");

        var result = await CliWorkspace.RunCli(workspace, null, "validate", "good.suite.json", "bad.suite.json");

        Assert.AreEqual(2, result.ExitCode, result.Output + result.Error);
        StringAssert.Contains(result.Output, "1 valid, 1 invalid");
    }

    [TestMethod]
    public async Task F6SecretsNeverReachAnyReportArtifact()
    {
        const string secret = "super-secret-acceptance-9000";
        await using var api = new LocalEchoApi();
        var workspace = CliWorkspace.Create();

        var result = await CliWorkspace.RunCli(
            workspace,
            null,
            "run",
            ExamplePath("redaction.suite.json"),
            "--env", $"baseUrl={api.BaseUrl}",
            "--env", $"apiToken={secret}",
            "--secret-env", "apiToken",
            "--report=standalone",
            "--report-out=artifact");

        Assert.AreEqual(0, result.ExitCode, result.Output + result.Error);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(workspace, "artifact")))
        {
            var content = File.ReadAllText(file);
            Assert.IsFalse(
                content.Contains(secret, StringComparison.Ordinal),
                $"Secret leaked into {Path.GetFileName(file)}");
        }

        Assert.IsFalse(result.Output.Contains(secret, StringComparison.Ordinal), "Secret leaked to stdout.");
    }

    [TestMethod]
    public async Task F7ReleaseMetadataIsConsistent()
    {
        Assert.IsTrue(File.Exists(Path.Combine(CliWorkspace.RepoRoot, "LICENSE")), "LICENSE must exist.");
        var buildProps = File.ReadAllText(Path.Combine(CliWorkspace.RepoRoot, "Directory.Build.props"));
        StringAssert.Contains(buildProps, "<Version>2.0.0-alpha.1</Version>");

        var help = await CliWorkspace.RunCli(CliWorkspace.Create(), null, "--help");
        Assert.AreEqual(0, help.ExitCode, help.Error);
        StringAssert.Contains(help.Output, "jtest 2.0.0");
    }

    [TestMethod]
    public async Task F8EnvParsingSplitsOnFirstEqualsAndRejectsDuplicates()
    {
        var workspace = CliWorkspace.Create();
        File.WriteAllText(
            Path.Combine(workspace, "env.suite.json"),
            """
            { "jtest": "2.0", "tests": [ { "name": "env", "steps": [
              { "type": "assert", "assert": [ { "op": "equals", "actual": "{{$.env.note}}", "expected": "a=b=c" } ] } ] } ] }
            """);

        var ok = await CliWorkspace.RunCli(workspace, null, "run", "env.suite.json", "--env", "note=a=b=c");
        Assert.AreEqual(0, ok.ExitCode, ok.Output + ok.Error);

        var duplicate = await CliWorkspace.RunCli(
            workspace, null, "run", "env.suite.json", "--env", "note=a=b=c", "--env", "note=other");
        Assert.AreEqual(2, duplicate.ExitCode, duplicate.Output + duplicate.Error);
        StringAssert.Contains(duplicate.Error + duplicate.Output, "Duplicate --env key");
    }

    [TestMethod]
    public async Task ParallelExecutionIsContentEquivalentToSequential()
    {
        var sequentialWorkspace = CliWorkspace.Create();
        var parallelWorkspace = CliWorkspace.Create();
        foreach (var workspace in new[] { sequentialWorkspace, parallelWorkspace })
        {
            for (var index = 0; index < 3; index++)
            {
                var expected = index == 1 ? 999 : index;
                File.WriteAllText(
                    Path.Combine(workspace, $"suite-{index}.suite.json"),
                    $$"""
                    { "jtest": "2.0", "info": { "name": "suite-{{index}}" }, "tests": [ { "name": "case", "steps": [
                      { "type": "assert", "assert": [ { "op": "equals", "actual": {{index}}, "expected": {{expected}} } ] } ] } ] }
                    """);
            }
        }

        var sequential = await CliWorkspace.RunCli(sequentialWorkspace, null, "run", "*.suite.json");
        var parallel = await CliWorkspace.RunCli(parallelWorkspace, null, "run", "*.suite.json", "--parallel", "4");

        Assert.AreEqual(1, sequential.ExitCode);
        Assert.AreEqual(1, parallel.ExitCode);

        static IReadOnlyList<(string Name, string Outcome)> SuiteOutcomes(JsonObject evidence) =>
            evidence["trace"]!["children"]!.AsArray()
                .Select(static suite => (
                    suite!["name"]!.GetValue<string>(),
                    suite["outcome"]!.GetValue<string>()))
                .OrderBy(static entry => entry.Item1, StringComparer.Ordinal)
                .ToList();

        CollectionAssert.AreEqual(
            SuiteOutcomes(sequential.ReadEvidence()).ToList(),
            SuiteOutcomes(parallel.ReadEvidence()).ToList());
        Assert.AreEqual(
            sequential.ReadEvidence()["counts"]!.ToJsonString(),
            parallel.ReadEvidence()["counts"]!.ToJsonString());
    }
}
