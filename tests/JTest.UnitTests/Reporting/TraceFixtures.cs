using JTest.Core.Tracing;

namespace JTest.UnitTests.Reporting;

/// <summary>Hand-authored ExecutionTrace fixtures for report tests (US2 is testable without live HTTP).</summary>
internal static class TraceFixtures
{
    private static Rollup R(params Outcome[] o) => Rollup.From(o);

    public static ExecutionTrace Mixed()
    {
        // A passing suite.
        var passStep = new StepNode
        {
            Id = "s0/c0/d0/step[1]", Path = "s0/c0/d0/step[1]", StepType = "http", Ordinal = 1,
            Name = "get-A", Outcome = Outcome.Passed,
            Assertions = new[] { new AssertionResult { Id = "a1", Operation = "equals", Expected = 200, Actual = 200, Outcome = Outcome.Passed } }
        };
        var passDataset = new DatasetResult { Id = "s0/c0/d0", Path = "s0/c0/d0", Label = "default", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Steps = new[] { passStep } };
        var passCase = new CaseResult { Id = "s0/c0", Path = "s0/c0", Name = "reads value", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Datasets = new[] { passDataset } };
        var passSuite = new SuiteResult { Id = "s0", Path = "s0", Name = "passing-suite", FilePath = "pass.json", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Cases = new[] { passCase } };

        // A failing suite: a passing login, a failing assertion, and a failing loop iteration.
        var login = new StepNode { Id = "s1/c0/d0/step[1]", Path = "s1/c0/d0/step[1]", StepType = "http", Ordinal = 1, Name = "login", Outcome = Outcome.Passed,
            Http = new HttpExchange { Method = "POST", Url = "https://example.test/auth/login", StatusCode = 200, Status = 200 },
            Assertions = new[] { new AssertionResult { Id = "la", Operation = "equals", Expected = 200, Actual = 200, Outcome = Outcome.Passed } } };
        var check = new StepNode { Id = "s1/c0/d0/step[2]", Path = "s1/c0/d0/step[2]", StepType = "http", Ordinal = 2, Name = "check-incident", Outcome = Outcome.Failed,
            Http = new HttpExchange { Method = "GET", Url = "https://example.test/incident", StatusCode = 500, Status = 500, ResponseBody = "{\"error\":\"boom\"}" },
            Assertions = new[] { new AssertionResult { Id = "ca", Operation = "equals", Expected = 200, Actual = 500, Outcome = Outcome.Failed, Message = "expected 200 but was 500" } } };
        var poll = new StepNode { Id = "s1/c0/d0/step[3]/iteration[0]/step[1]", Path = "s1/c0/d0/step[3]/iteration[0]/step[1]", StepType = "http", Ordinal = 1, Name = "poll", Outcome = Outcome.Failed };
        var iter0 = new Iteration { Id = "s1/c0/d0/step[3]/iteration[0]", Path = "s1/c0/d0/step[3]/iteration[0]", Index = 0, Outcome = Outcome.Failed, Steps = new[] { poll } };
        var loop = new StepNode { Id = "s1/c0/d0/step[3]", Path = "s1/c0/d0/step[3]", Kind = NodeKind.Loop, StepType = "for", Ordinal = 3, Name = "retry", Outcome = Outcome.Failed, Iterations = new[] { iter0 } };

        var failDataset = new DatasetResult { Id = "s1/c0/d0", Path = "s1/c0/d0", Label = "default", Outcome = Outcome.Failed, Counts = R(Outcome.Passed, Outcome.Failed, Outcome.Failed), Steps = new[] { login, check, loop } };
        var failCase = new CaseResult { Id = "s1/c0", Path = "s1/c0", Name = "authenticated flow", Outcome = Outcome.Failed, Counts = R(Outcome.Failed), Datasets = new[] { failDataset } };
        var failSuite = new SuiteResult { Id = "s1", Path = "s1", Name = "failing-suite", FilePath = "fail.json", Outcome = Outcome.Failed, Counts = R(Outcome.Failed), Cases = new[] { failCase } };

        // Deliberately list the passing suite FIRST — the generator must reorder failure-first.
        return new ExecutionTrace
        {
            ToolVersion = "2.0.0", StartedAt = DateTimeOffset.UnixEpoch, EndedAt = DateTimeOffset.UnixEpoch, DurationMs = 42,
            Outcome = Outcome.Failed, ExitCode = 1, Counts = R(Outcome.Passed, Outcome.Failed),
            Suites = new[] { passSuite, failSuite }
        };
    }

    public static ExecutionTrace WithResponseBody(string body)
    {
        var step = new StepNode { Id = "s0/c0/d0/step[1]", Path = "s0/c0/d0/step[1]", StepType = "http", Ordinal = 1, Name = "get", Outcome = Outcome.Passed,
            Http = new HttpExchange { Method = "GET", Url = "https://example.test/data", StatusCode = 200, Status = 200, ResponseBody = body } };
        var ds = new DatasetResult { Id = "s0/c0/d0", Path = "s0/c0/d0", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Steps = new[] { step } };
        var c = new CaseResult { Id = "s0/c0", Path = "s0/c0", Name = "downloads", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Datasets = new[] { ds } };
        var s = new SuiteResult { Id = "s0", Path = "s0", Name = "suite", Outcome = Outcome.Passed, Counts = R(Outcome.Passed), Cases = new[] { c } };
        return new ExecutionTrace { ToolVersion = "2.0.0", StartedAt = DateTimeOffset.UnixEpoch, EndedAt = DateTimeOffset.UnixEpoch, Outcome = Outcome.Passed, ExitCode = 0, Counts = R(Outcome.Passed), Suites = new[] { s } };
    }

    public static string ExtractEmbeddedTrace(string html)
    {
        const string marker = "id=\"jtest-trace\">";
        var start = html.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        return html[start..end];
    }
}
