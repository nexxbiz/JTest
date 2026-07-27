using JTest.Cli.Commands;
using JTest.Cli.Loading;
using JTest.Cli.Ports;
using JTest.Engine.Execution;
using JTest.Engine.Ports;
using JTest.Language.Reading;
using JTest.Language.Semantics;
using JTest.Reporting.Canonical;
using JTest.Reporting.Writers;
using Orbyss.ProgramKit.Serialization.Json.Canonicalization;

namespace JTest.Cli.Composition;

/// <summary>The jtest composition root: builds the command router with real collaborators.</summary>
public static class CliComposition
{
    /// <summary>Creates the router over one caller-owned HTTP client.</summary>
    /// <param name="httpClient">The client used by http steps; owned by the caller.</param>
    public static CliCommandRouter CreateRouter(HttpClient httpClient)
    {
        var console = new SystemConsoleWriter();
        var loader = new SuiteBundleLoader(
            new SuiteDocumentReader(),
            new TemplateFileReader(),
            new SuiteBundleValidator());
        var runner = new SuiteRunner(
            new HttpClientTransport(httpClient),
            new SystemEngineClock(),
            new TaskDelayScheduler(),
            new SystemProcessEnvironment());
        var run = new RunCliCommand(
            loader,
            runner,
            new ResultDocumentWriter(new ProgramKitJsonCanonicalizer()),
            new CatalogReportWriter(),
            new StandaloneReportWriter(),
            console,
            new ProcessStartReportOpener());

        return new CliCommandRouter(
            run,
            new ValidateCliCommand(loader, console),
            new DescribeCliCommand(console),
            console);
    }
}
