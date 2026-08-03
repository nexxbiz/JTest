using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Execution;

public class ExecutionTraceAssemblerTests
{
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
}
