using JTest.Cli.Commands;
using JTest.Cli.Console;
using JTest.Cli.Invocation;
using JTest.Cli.Loading;
using JTest.Cli.Tests.TestSupport;
using JTest.Language.Reading;
using JTest.Language.Semantics;

namespace JTest.Cli.Tests.Console;

[TestClass]
public sealed class JTestHandlerAdaptationTests
{
    [TestMethod]
    public async Task DescribeHandlerReturnsTheRoutedExitCodeAndOutput()
    {
        var console = new RecordingConsole();
        var handler = new JTestDescribeHandler(Router(console), new StubSession());

        var exitCode = await handler.HandleAsync(
            new JTestDescribeRequest("manifest", "-"),
            CancellationToken.None);

        Assert.AreEqual(CliExitCodes.Passed, exitCode);
        Assert.IsTrue(console.OutLines.Single().Contains("\"language\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DescribeHandlerReportsUnknownSchemaAsInvalidInput()
    {
        var console = new RecordingConsole();
        var handler = new JTestDescribeHandler(Router(console), new StubSession());

        var exitCode = await handler.HandleAsync(
            new JTestDescribeRequest("nonsense", "-"),
            CancellationToken.None);

        Assert.AreEqual(CliExitCodes.InvalidInput, exitCode);
        Assert.IsTrue(console.ErrorLines.Single().StartsWith("Unknown schema", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ValidateHandlerReportsUnmatchedPatternsAsInvalidInput()
    {
        var console = new RecordingConsole();
        var handler = new JTestValidateHandler(Router(console), new StubSession());

        var exitCode = await handler.HandleAsync(
            new JTestValidateRequest(["absent.json"], "text"),
            CancellationToken.None);

        Assert.AreEqual(CliExitCodes.InvalidInput, exitCode);
        Assert.AreEqual("No suite files matched: absent.json", console.ErrorLines.Single());
    }

    private static CliCommandRouter Router(RecordingConsole console)
    {
        var loader = new SuiteBundleLoader(
            new SuiteDocumentReader(),
            new TemplateFileReader(),
            new SuiteBundleValidator());
        return new CliCommandRouter(
            run: null!,
            new ValidateCliCommand(loader, console),
            new DescribeCliCommand(console),
            console);
    }

    private sealed class StubSession : IJTestCliSession
    {
        public CliEnvironment Capture() => new(
            Directory.CreateTempSubdirectory("jtest-handler-").FullName,
            "true",
            true);
    }
}
