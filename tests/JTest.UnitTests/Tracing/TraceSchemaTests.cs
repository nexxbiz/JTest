using System.Text;
using System.Text.Json;
using Json.Schema;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Tracing;

public class TraceSchemaTests
{
    [Fact]
    public void BuiltTrace_SerializesAndValidatesAgainstSchema()
    {
        var trace = SampleTrace();
        var json = TraceJson.Serialize(trace);
        using var doc = JsonDocument.Parse(json);

        var schema = JsonSchema.FromText(File.ReadAllText(SchemaPath()));
        var results = schema.Evaluate(doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(results.IsValid, Describe(results, json));
    }

    [Fact]
    public void Trace_RoundTrips()
    {
        var json = TraceJson.Serialize(SampleTrace());
        var back = TraceJson.Deserialize(json);
        Assert.NotNull(back);
        Assert.Equal("2.0.0", back!.ToolVersion);
        Assert.Single(back.Suites);
    }

    /// <summary>A small trace exercising suite→case→dataset→step, a loop→iteration→step, and an assertion.</summary>
    private static ExecutionTrace SampleTrace()
    {
        var suitePath = TraceBuilder.Path("", "suite", 0);
        var casePath = TraceBuilder.Path(suitePath, "case", 0);
        var datasetPath = TraceBuilder.Path(casePath, "dataset", 0);
        var step0Path = TraceBuilder.Path(datasetPath, "step", 1);
        var loopPath = TraceBuilder.Path(datasetPath, "step", 2);
        var iterPath = TraceBuilder.Path(loopPath, "iteration", 0);
        var innerPath = TraceBuilder.Path(iterPath, "step", 1);

        var assertion = new AssertionResult
        {
            Id = TraceBuilder.Path(innerPath, "assert", 1),
            Operation = "equals",
            Expected = 200,
            Actual = 200,
            Outcome = Outcome.Passed
        };

        var innerStep = new StepNode
        {
            Id = innerPath, Path = innerPath, Kind = NodeKind.Step, StepType = "http",
            Ordinal = 1, Outcome = Outcome.Passed, Assertions = new[] { assertion }
        };

        var iteration = new Iteration
        {
            Id = iterPath, Path = iterPath, Index = 0, Outcome = Outcome.Passed,
            Steps = new[] { innerStep }
        };

        var loop = new StepNode
        {
            Id = loopPath, Path = loopPath, Kind = NodeKind.Loop, StepType = "for",
            Ordinal = 2, Outcome = Outcome.Passed, Iterations = new[] { iteration }
        };

        var step0 = new StepNode
        {
            Id = step0Path, Path = step0Path, Kind = NodeKind.Step, StepType = "http",
            Ordinal = 1, Outcome = Outcome.Passed
        };

        var dataset = new DatasetResult
        {
            Id = datasetPath, Path = datasetPath, Label = "default",
            Outcome = Outcome.Passed, Counts = Rollup.From(new[] { Outcome.Passed, Outcome.Passed }),
            Steps = new[] { step0, loop }
        };

        var @case = new CaseResult
        {
            Id = casePath, Path = casePath, Name = "logs in",
            Outcome = Outcome.Passed, Counts = Rollup.From(new[] { Outcome.Passed }),
            Datasets = new[] { dataset }
        };

        var suite = new SuiteResult
        {
            Id = suitePath, Path = suitePath, Name = "auth suite", FilePath = "auth.json",
            Outcome = Outcome.Passed, Counts = Rollup.From(new[] { Outcome.Passed }),
            Cases = new[] { @case }
        };

        return new ExecutionTrace
        {
            ToolVersion = "2.0.0",
            StartedAt = DateTimeOffset.UnixEpoch,
            EndedAt = DateTimeOffset.UnixEpoch,
            DurationMs = 12,
            Outcome = Outcome.Passed,
            ExitCode = 0,
            Counts = Rollup.From(new[] { Outcome.Passed }),
            Suites = new[] { suite }
        };
    }

    private static string SchemaPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JTest.sln")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "specs", "001-jtest2-pipeline-reporting",
            "contracts", "execution-trace.schema.json");
    }

    private static string Describe(EvaluationResults results, string json)
    {
        if (results.IsValid) return string.Empty;

        var sb = new StringBuilder("Trace did not validate against execution-trace.schema.json:\n");
        foreach (var detail in results.Details)
        {
            if (detail.IsValid || detail.Errors is null) continue;
            foreach (var error in detail.Errors)
                sb.AppendLine($"  at {detail.InstanceLocation}: {error.Key} → {error.Value}");
        }
        sb.AppendLine("--- serialized trace ---");
        sb.AppendLine(json);
        return sb.ToString();
    }
}
