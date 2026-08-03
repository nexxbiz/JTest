using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Security;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Execution;

public class ExecutionTraceAssemblerTests
{
    [Fact]
    public void Assemble_RedactsCookieAuthAndSecrets_AndExposesStatusCodeAndStatus()
    {
        var data = new Dictionary<string, object?>
        {
            ["statusCode"] = 200,
            ["status"] = 200,
            ["headers"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json",
                ["Set-Cookie"] = new object?[] { "session=abc; HttpOnly", "csrf=xyz" }
            },
            ["body"] = "echoed: Bearer sk-live-123",
            ["request"] = new Dictionary<string, object?>
            {
                ["method"] = "GET",
                ["url"] = "https://api.test/thing",
                ["headers"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Authorization"] = "Bearer sk-live-123"
                },
                ["body"] = null
            }
        };

        var step = new StepProcessedResult(1) { Step = new FakeHttpStep(), Success = true, Data = data };
        var caseResult = new JTestCaseResult { TestCaseName = "auth" };
        caseResult.AddStepResult(step);
        var suite = new JTestSuiteExecutionResult("f.json", "s", null, new[] { caseResult });

        var trace = ExecutionTraceAssembler.Assemble(
            new[] { suite }, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
        var http = trace.Suites[0].Cases[0].Datasets[0].Steps[0].Http!;

        Assert.Equal(200, http.StatusCode);
        Assert.Equal(200, http.Status);
        Assert.Equal("application/json", http.ResponseHeaders!["Content-Type"]);   // non-secret preserved
        Assert.Equal(ValueRedactor.Mask, http.ResponseHeaders!["Set-Cookie"]);     // cookie masked
        Assert.Equal(ValueRedactor.Mask, http.RequestHeaders!["Authorization"]);   // authorization masked
        Assert.Equal($"echoed: {ValueRedactor.Mask}", http.ResponseBody);          // token also masked in the body
    }

    [Fact]
    public void Assemble_LoopStep_ProducesIterationNodes_AndValidatesAgainstSchema()
    {
        var inner = new StepProcessedResult(1) { Step = new WaitStep(new(Ms: 1)), Success = true };
        var iteration = new StepIteration(0, true, new[] { inner });
        var forConfig = new ForLoopStepConfiguration(new object[] { "a" }, [new WaitStep(new(Ms: 1))]);
        var loop = new StepProcessedResult(1)
        {
            Step = new ForLoopStep(StepProcessor.Default, forConfig),
            Success = true,
            Iterations = new[] { iteration }
        };

        var caseResult = new JTestCaseResult { TestCaseName = "loops" };
        caseResult.AddStepResult(loop);
        var suite = new JTestSuiteExecutionResult("f.json", "suite", null, new[] { caseResult });

        var trace = ExecutionTraceAssembler.Assemble(
            new[] { suite }, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        var stepNode = trace.Suites[0].Cases[0].Datasets[0].Steps[0];
        Assert.Equal(NodeKind.Loop, stepNode.Kind);
        Assert.Equal("for", stepNode.StepType);
        Assert.NotNull(stepNode.Iterations);
        Assert.Single(stepNode.Iterations!);
        Assert.Single(stepNode.Iterations!.First().Steps);

        // Serializes without error (schema conformance of the trace model is covered by TraceSchemaTests).
        Assert.Contains("\"kind\": \"loop\"", TraceJson.Serialize(trace));
    }

    [Fact]
    public void Assemble_CrashingSuite_IsErroredNode_WithDiagnostic()
    {
        var errored = new JTestSuiteExecutionResult("bad.json", null, null, Array.Empty<JTestCaseResult>())
        {
            ExecutionError = "boom"
        };

        var trace = ExecutionTraceAssembler.Assemble(
            new[] { errored }, "2.0.0", 2, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        Assert.Equal(Outcome.Errored, trace.Suites[0].Outcome);
        Assert.NotNull(trace.Suites[0].Diagnostics);
        Assert.Contains("boom", trace.Suites[0].Diagnostics!.First().Message);
        Assert.Equal(Outcome.Errored, trace.Outcome);
    }

    [Fact]
    public void Assemble_TimedOutStep_PropagatesTimedOut_AndYieldsExitCode4()
    {
        var step = new StepProcessedResult(1)
        {
            Step = new WaitStep(new(Ms: 1)),
            Success = false,
            TimedOut = true
        };
        var caseResult = new JTestCaseResult { TestCaseName = "waits" };
        caseResult.AddStepResult(step);
        var suite = new JTestSuiteExecutionResult("f.json", "s", null, new[] { caseResult });

        var trace = ExecutionTraceAssembler.Assemble(
            new[] { suite }, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        // Timeout is a distinct outcome that propagates step → case → suite.
        Assert.Equal(Outcome.TimedOut, trace.Suites[0].Cases[0].Datasets[0].Steps[0].Outcome);
        Assert.Equal(Outcome.TimedOut, trace.Suites[0].Cases[0].Outcome);
        Assert.Equal(Outcome.TimedOut, trace.Suites[0].Outcome);

        // ...and drives the "aborted" exit code (4), never a false green.
        Assert.Equal(4, ExitCodeService.From(trace.Counts));
    }

    private sealed class FakeHttpStep : IStep
    {
        public string TypeName => "http";
        public IStepConfiguration Configuration { get; } = new FakeConfig();

        public Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool Validate(IExecutionContext context, out IEnumerable<string> validationErrors)
        {
            validationErrors = Array.Empty<string>();
            return true;
        }

        private sealed class FakeConfig : IStepConfiguration
        {
            public string? Id => null;
            public string? Name => null;
            public string? Description => null;
            public IEnumerable<IAssertionOperation>? Assert => null;
            public IReadOnlyDictionary<string, object?>? Save => null;
        }
    }
}
