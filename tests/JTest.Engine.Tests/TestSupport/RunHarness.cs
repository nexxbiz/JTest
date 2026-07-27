using JTest.Engine.Execution;
using JTest.Engine.Ports;
using JTest.Engine.Tracing;
using JTest.Language.Reading;
using JTest.Language.Semantics;

namespace JTest.Engine.Tests.TestSupport;

/// <summary>Builds and executes suites from JSON with fully scripted ports.</summary>
internal static class RunHarness
{
    internal static readonly DateTimeOffset Start =
        new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    internal static async Task<TraceNode> Run(
        string suiteJson,
        FakeHttpTransport transport,
        IProcessEnvironment? environment = null,
        RunOptions? options = null,
        string? templatesJson = null,
        CancellationToken cancellationToken = default)
    {
        var reader = new SuiteDocumentReader();
        var read = reader.Read("suite.json", suiteJson);
        Assert.IsTrue(read.IsValid, string.Join("; ", read.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));

        var templateFiles = new List<(string, JTest.Language.Documents.TemplateFileDocument)>();
        if (templatesJson is not null)
        {
            var templateReader = new TemplateFileReader();
            var templates = templateReader.Read("templates.json", templatesJson);
            Assert.IsTrue(templates.IsValid, string.Join("; ", templates.Diagnostics.Select(d => d.Message)));
            templateFiles.Add(("templates.json", templates.Document!));
        }

        var bundle = new SuiteBundle("suite.json", read.Document!, templateFiles);
        var runner = new SuiteRunner(
            transport,
            new FixedClock(Start),
            new NoDelayScheduler(),
            environment ?? new FakeProcessEnvironment());

        return await runner.ExecuteRun([bundle], options ?? new RunOptions(), cancellationToken);
    }

    /// <summary>Asserts the structural invariant: no node passes with a non-passed child.</summary>
    internal static void AssertNoFalseGreen(TraceNode node)
    {
        if (node.Outcome == TraceOutcome.Passed)
        {
            foreach (var child in node.Children)
            {
                Assert.AreEqual(
                    TraceOutcome.Passed,
                    child.Outcome,
                    $"Node '{node.Path}' passed but child '{child.Path}' is {child.Outcome}.");
            }
        }

        foreach (var child in node.Children)
        {
            AssertNoFalseGreen(child);
        }
    }

    internal static TraceNode Descend(TraceNode node, params int[] childIndexes)
    {
        var current = node;
        foreach (var index in childIndexes)
        {
            current = current.Children[index];
        }

        return current;
    }
}
