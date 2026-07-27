using System.Text;
using System.Text.Json.Nodes;
using JTest.Engine.Tracing;
using JTest.Reporting.Canonical;
using JTest.Reporting.Viewer;
using JTest.Reporting.Writers;
using Orbyss.ProgramKit.Serialization.Json.Canonicalization;

namespace JTest.Reporting.Tests.Writers;

[TestClass]
public sealed class ReportWriterTests
{
    private static ResultDocument BuildDocument(string caseName, int minutesOffset = 0)
    {
        var start = new DateTimeOffset(2026, 7, 27, 12, minutesOffset, 0, TimeSpan.Zero);
        var step = new TraceNode(TraceNodeKind.Step, "suites/0/cases/0/steps/0", 1)
        {
            StepType = "wait",
            StartUtc = start,
            DurationMs = 1,
        };
        var caseNode = new TraceNode(TraceNodeKind.Case, "suites/0/cases/0", 1)
        {
            DisplayName = caseName,
            StartUtc = start,
            DurationMs = 2,
        };
        caseNode.AddChild(step);
        caseNode.SealFromChildren();
        var suite = new TraceNode(TraceNodeKind.Suite, "suites/0", 1)
        {
            DisplayName = "suite",
            StartUtc = start,
            DurationMs = 3,
        };
        suite.AddChild(caseNode);
        suite.SealFromChildren();
        var run = new TraceNode(TraceNodeKind.Run, string.Empty, 1) { StartUtc = start, DurationMs = 4 };
        run.AddChild(suite);
        run.SealFromChildren();

        return new ResultDocumentWriter(new ProgramKitJsonCanonicalizer()).Write(run);
    }

    private static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "jtest-writer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [TestMethod]
    public void CatalogWriteIsDeterministicAndUpdatesTheIndex()
    {
        var first = TempDir();
        var second = TempDir();
        var writer = new CatalogReportWriter();
        var documentA = BuildDocument("alpha");
        var documentB = BuildDocument("beta", minutesOffset: 5);

        writer.Write(documentA, first);
        writer.Write(documentB, first);

        writer.Write(documentA, second);
        writer.Write(documentB, second);

        foreach (var relative in new[]
                 {
                     "catalog.js", "index.html", "viewer.css", "viewer.js",
                     $"runs/{documentA.RunId}/result.js", $"runs/{documentA.RunId}/result.json",
                 })
        {
            CollectionAssert.AreEqual(
                File.ReadAllBytes(Path.Combine(first, relative)),
                File.ReadAllBytes(Path.Combine(second, relative)),
                $"Nondeterministic bytes: {relative}");
        }

        var catalogText = File.ReadAllText(Path.Combine(first, "catalog.js"));
        StringAssert.StartsWith(catalogText, "window.__JTEST_CATALOG__ = ");
        var catalog = JsonNode.Parse(
            catalogText["window.__JTEST_CATALOG__ = ".Length..].TrimEnd(';', '\n'))!.AsObject();
        var runs = catalog["runs"]!.AsArray();
        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual(documentB.RunId, runs[0]!["runId"]!.GetValue<string>(), "Newest run first.");
        Assert.AreEqual(documentA.RunId, runs[1]!["runId"]!.GetValue<string>());
    }

    [TestMethod]
    public void RewritingTheSameRunDoesNotDuplicateCatalogEntries()
    {
        var directory = TempDir();
        var writer = new CatalogReportWriter();
        var document = BuildDocument("alpha");

        writer.Write(document, directory);
        var once = File.ReadAllBytes(Path.Combine(directory, "catalog.js"));
        writer.Write(document, directory);
        var twice = File.ReadAllBytes(Path.Combine(directory, "catalog.js"));

        CollectionAssert.AreEqual(once, twice);
    }

    [TestMethod]
    public void StandaloneReportIsSelfContainedAndDeterministic()
    {
        var first = TempDir();
        var second = TempDir();
        var writer = new StandaloneReportWriter();
        var document = BuildDocument("alpha");

        var result = writer.Write(document, first);
        writer.Write(document, second);

        CollectionAssert.AreEqual(
            File.ReadAllBytes(result.IndexHtmlPath),
            File.ReadAllBytes(Path.Combine(second, "index.html")));

        var html = File.ReadAllText(result.IndexHtmlPath);
        Assert.IsFalse(html.Contains("src=\"viewer.js\"", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("href=\"viewer.css\"", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("window.__JTEST_RUN__", StringComparison.Ordinal));
        CollectionAssert.AreEqual(document.CanonicalBytes, File.ReadAllBytes(result.ResultJsonPath));
    }

    [TestMethod]
    public void HostileDataCannotBreakOutOfTheInlineScript()
    {
        var start = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var step = new TraceNode(TraceNodeKind.Step, "suites/0/cases/0/steps/0", 1)
        {
            StepType = "http",
            DisplayName = "</script><script>alert(1)</script>",
            StartUtc = start,
            Evidence = new JsonObject
            {
                ["response"] = new JsonObject
                {
                    ["body"] = new JsonObject { ["payload"] = "</script><img src=x onerror=alert(2)>" },
                },
            },
        };
        var run = new TraceNode(TraceNodeKind.Run, string.Empty, 1) { StartUtc = start };
        run.AddChild(step);
        run.SealFromChildren();
        var document = new ResultDocumentWriter(new ProgramKitJsonCanonicalizer()).Write(run);

        var html = new StandaloneReportWriter()
            .Write(document, TempDir())
            .IndexHtmlPath;
        var text = File.ReadAllText(html);

        var dataStart = text.IndexOf("window.__JTEST_RUN__", StringComparison.Ordinal);
        var dataEnd = text.IndexOf("</script>", dataStart, StringComparison.Ordinal);
        var inlineData = text[dataStart..dataEnd];
        Assert.IsFalse(
            inlineData.Contains("</script", StringComparison.OrdinalIgnoreCase),
            "Hostile data must not be able to terminate the inline script element.");
        Assert.IsTrue(inlineData.Contains("<\\/script", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewerNeverUsesMarkupInjectionOrExternalRequests()
    {
        foreach (var forbidden in new[] { "innerHTML", "outerHTML", "document.write", "insertAdjacentHTML", "eval(" })
        {
            Assert.IsFalse(
                ViewerAssets.ViewerJs.Contains(forbidden, StringComparison.Ordinal),
                $"viewer.js must not use {forbidden}.");
        }

        foreach (var asset in new[] { ViewerAssets.IndexHtml, ViewerAssets.ViewerCss, ViewerAssets.ViewerJs })
        {
            Assert.IsFalse(asset.Contains("http://", StringComparison.Ordinal), "No external requests.");
            Assert.IsFalse(asset.Contains("https://", StringComparison.Ordinal), "No external requests.");
        }
    }
}
