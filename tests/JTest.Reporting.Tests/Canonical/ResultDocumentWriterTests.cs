using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using JTest.Engine.Tracing;
using JTest.Reporting.Canonical;
using Orbyss.ProgramKit.Serialization.Json.Canonicalization;

namespace JTest.Reporting.Tests.Canonical;

[TestClass]
public sealed class ResultDocumentWriterTests
{
    private static readonly JsonSchema ResultSchema = JsonSchema.FromText(ReportingContract.ResultSchemaJson);

    private static TraceNode BuildRun()
    {
        var start = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

        var assertion = new TraceNode(TraceNodeKind.Assertion, "suites/0/cases/0/steps/0/assertions/0", 1)
        {
            StartUtc = start,
            Evidence = new JsonObject { ["op"] = "equals", ["actual"] = 200, ["expected"] = 200, ["message"] = "" },
        };
        assertion.RecordOutcome(TraceOutcome.Passed);

        var step = new TraceNode(TraceNodeKind.Step, "suites/0/cases/0/steps/0", 1)
        {
            StepType = "http",
            StepId = "fetch",
            DisplayName = "Fetch",
            StartUtc = start,
            DurationMs = 12.5,
            Evidence = new JsonObject { ["request"] = new JsonObject { ["method"] = "GET", ["url"] = "https://x.test" } },
        };
        step.AddChild(assertion);
        step.SealFromChildren();

        var caseNode = new TraceNode(TraceNodeKind.Case, "suites/0/cases/0", 1)
        {
            DisplayName = "case one",
            StartUtc = start,
            DurationMs = 13,
        };
        caseNode.AddChild(step);
        caseNode.SealFromChildren();

        var suite = new TraceNode(TraceNodeKind.Suite, "suites/0", 1)
        {
            DisplayName = "suite",
            StartUtc = start,
            DurationMs = 14,
            Evidence = new JsonObject { ["source"] = "suite.json" },
        };
        suite.AddChild(caseNode);
        suite.SealFromChildren();

        var run = new TraceNode(TraceNodeKind.Run, string.Empty, 1)
        {
            StartUtc = start,
            DurationMs = 15,
        };
        run.AddChild(suite);
        run.SealFromChildren();
        return run;
    }

    [TestMethod]
    public void IdenticalTracesYieldByteIdenticalDocuments()
    {
        var writer = new ResultDocumentWriter(new ProgramKitJsonCanonicalizer());

        var first = writer.Write(BuildRun());
        var second = writer.Write(BuildRun());

        CollectionAssert.AreEqual(first.CanonicalBytes, second.CanonicalBytes);
        Assert.AreEqual(first.RunId, second.RunId);
        Assert.AreEqual(first.Digest, second.Digest);
    }

    [TestMethod]
    public void DocumentConformsToThePublishedResultSchema()
    {
        var writer = new ResultDocumentWriter(new ProgramKitJsonCanonicalizer());
        var document = writer.Write(BuildRun());

        using var parsed = JsonDocument.Parse(Encoding.UTF8.GetString(document.CanonicalBytes));
        var evaluation = ResultSchema.Evaluate(
            parsed.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List });

        var details = evaluation.Details ?? [];
        Assert.IsTrue(
            evaluation.IsValid,
            string.Join("; ", details.Where(d => d.Errors is { Count: > 0 })
                .SelectMany(d => d.Errors!.Values.Select(e => $"{d.InstanceLocation}: {e}"))));
    }

    [TestMethod]
    public void CountsReflectCaseRunsAndAssertions()
    {
        var writer = new ResultDocumentWriter(new ProgramKitJsonCanonicalizer());
        var document = writer.Write(BuildRun());

        var root = JsonNode.Parse(Encoding.UTF8.GetString(document.CanonicalBytes))!;
        Assert.AreEqual("passed", root["outcome"]!.GetValue<string>());
        Assert.AreEqual(1, root["counts"]!["caseRuns"]!["total"]!.GetValue<int>());
        Assert.AreEqual(1, root["counts"]!["caseRuns"]!["passed"]!.GetValue<int>());
        Assert.AreEqual(1, root["counts"]!["assertions"]!["total"]!.GetValue<int>());
        Assert.AreEqual(0, root["counts"]!["assertions"]!["failed"]!.GetValue<int>());
        Assert.AreEqual(root["runId"]!.GetValue<string>(), document.RunId);
    }
}
