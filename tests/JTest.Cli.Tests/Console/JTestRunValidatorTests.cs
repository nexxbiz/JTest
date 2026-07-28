using JTest.Cli.Commands;
using JTest.Cli.Console;

namespace JTest.Cli.Tests.Console;

[TestClass]
public sealed class JTestRunValidatorTests
{
    [TestMethod]
    public async Task ReportsUnmatchedPatternsThroughTheResult()
    {
        using var workspace = new TempWorkspace();
        var validator = new JTestRunValidator(workspace.Session);

        var result = await validator.ValidateAsync(
            Request(workspace, patterns: ["missing.json"]),
            CancellationToken.None);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("No suite files matched: missing.json", result.Messages.Single());
    }

    [TestMethod]
    public async Task ReportsInvalidEnvPairThroughTheResult()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteSuite("suite.json");
        var validator = new JTestRunValidator(workspace.Session);

        var result = await validator.ValidateAsync(
            Request(workspace, env: ["novalue"]),
            CancellationToken.None);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Invalid --env value 'novalue'; expected key=value.", result.Messages.Single());
    }

    [TestMethod]
    public async Task ReportsMissingValueFileThroughTheResult()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteSuite("suite.json");
        var validator = new JTestRunValidator(workspace.Session);

        var result = await validator.ValidateAsync(
            Request(workspace, envFile: "absent.json"),
            CancellationToken.None);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Value file 'absent.json' does not exist.", result.Messages.Single());
    }

    [TestMethod]
    public async Task ReportsInvalidParallelismThroughTheResult()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteSuite("suite.json");
        var validator = new JTestRunValidator(workspace.Session);

        var result = await validator.ValidateAsync(
            Request(workspace, parallel: "zero"),
            CancellationToken.None);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Invalid --parallel value 'zero'; expected a positive integer.", result.Messages.Single());
    }

    [TestMethod]
    public async Task AcceptsBindableInputs()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteSuite("suite.json");
        var validator = new JTestRunValidator(workspace.Session);

        var result = await validator.ValidateAsync(
            Request(workspace, env: ["key=value"]),
            CancellationToken.None);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Messages.Count);
    }

    private static JTestRunRequest Request(
        TempWorkspace workspace,
        IReadOnlyList<string>? patterns = null,
        string? envFile = null,
        IReadOnlyList<string>? env = null,
        string parallel = "1") =>
        new(
            [.. patterns ?? ["suite.json"]],
            envFile,
            [.. env ?? []],
            null,
            [],
            "catalog",
            null,
            null,
            parallel,
            null,
            false,
            false,
            "text");

    private sealed class TempWorkspace : IDisposable
    {
        private readonly string directory = Directory.CreateTempSubdirectory("jtest-validator-").FullName;

        internal IJTestCliSession Session => new FixedSession(directory);

        internal void WriteSuite(string name) => File.WriteAllText(
            Path.Combine(directory, name),
            """{"version":"2.0.0","suite":"probe","cases":[]}""");

        public void Dispose() => Directory.Delete(directory, recursive: true);
    }

    private sealed class FixedSession : IJTestCliSession
    {
        private readonly string workingDirectory;

        internal FixedSession(string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }

        public CliEnvironment Capture() => new(workingDirectory, null, true);
    }
}
