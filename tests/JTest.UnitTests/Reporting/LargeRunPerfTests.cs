using System.Diagnostics;
using JTest.Core.Reporting.Html;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Reporting;

public class LargeRunPerfTests
{
    [Fact]
    public void LargeReport_SerializesAndRenders_WithinBudget()
    {
        var trace = BuildLarge(suites: 50, stepsPerCase: 100); // ~5,000 step nodes

        var sw = Stopwatch.StartNew();
        var html = new HtmlReportGenerator().Generate(trace);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 3000, $"HTML render took {sw.ElapsedMilliseconds} ms (budget 3000 ms)");
        Assert.True(html.Length > 0);
    }

    private static ExecutionTrace BuildLarge(int suites, int stepsPerCase)
    {
        var suiteResults = new List<SuiteResult>();
        for (var si = 0; si < suites; si++)
        {
            var suitePath = TraceBuilder.Path("", "suite", si);
            var casePath = TraceBuilder.Path(suitePath, "case", 0);
            var datasetPath = TraceBuilder.Path(casePath, "dataset", 0);

            var steps = new List<StepNode>();
            for (var st = 0; st < stepsPerCase; st++)
            {
                var p = TraceBuilder.Path(datasetPath, "step", st + 1);
                steps.Add(new StepNode { Id = p, Path = p, StepType = "http", Ordinal = st + 1, Outcome = Outcome.Passed });
            }

            var counts = Rollup.From(steps.Select(s => s.Outcome));
            var dataset = new DatasetResult { Id = datasetPath, Path = datasetPath, Outcome = Outcome.Passed, Counts = counts, Steps = steps };
            var @case = new CaseResult { Id = casePath, Path = casePath, Name = $"case-{si}", Outcome = Outcome.Passed, Counts = Rollup.From(new[] { Outcome.Passed }), Datasets = new[] { dataset } };
            suiteResults.Add(new SuiteResult { Id = suitePath, Path = suitePath, Name = $"suite-{si}", Outcome = Outcome.Passed, Counts = Rollup.From(new[] { Outcome.Passed }), Cases = new[] { @case } });
        }

        return new ExecutionTrace
        {
            ToolVersion = "2.0.0",
            StartedAt = DateTimeOffset.UnixEpoch,
            EndedAt = DateTimeOffset.UnixEpoch,
            Outcome = Outcome.Passed,
            ExitCode = 0,
            Counts = Rollup.From(suiteResults.Select(s => s.Outcome)),
            Suites = suiteResults
        };
    }
}
