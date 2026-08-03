using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.Tracing;
using JTest.Core.Variables;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.Execution;

public class AncestryAndParallelTests
{
    private static StepProcessedResult Wait(bool success = true) =>
        new(1) { Step = new WaitStep(new(Ms: 1)), Success = success };

    // T048: deep template + loop nesting yields unique ids / correct ancestry (no collisions).
    [Fact]
    public void NestedTrace_HasUniqueNodeIds()
    {
        var templateChild = Wait();
        var template = new StepProcessedResult(1)
        {
            Step = new WaitStep(new(Ms: 1)), Success = true, InnerResults = new[] { templateChild }
        };

        var loopInner = Wait();
        var loop = new StepProcessedResult(1)
        {
            Step = new WaitStep(new(Ms: 1)), Success = true,
            Iterations = new[] { new StepIteration(0, true, new[] { loopInner }), new StepIteration(1, true, new[] { Wait() }) }
        };

        var caseResult = new JTestCaseResult { TestCaseName = "nested" };
        caseResult.AddStepResult(template);
        caseResult.AddStepResult(loop);
        var suite = new JTestSuiteExecutionResult("f.json", "s", null, new[] { caseResult });

        var trace = ExecutionTraceAssembler.Assemble(
            new[] { suite }, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        var ids = new List<string>();
        Collect(trace, ids);

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct().Count()); // every node id is unique
    }

    private static void Collect(ExecutionTrace t, List<string> ids)
    {
        foreach (var s in t.Suites) { ids.Add(s.Id); foreach (var c in s.Cases) CollectCase(c, ids); }
    }
    private static void CollectCase(CaseResult c, List<string> ids)
    {
        ids.Add(c.Id);
        foreach (var d in c.Datasets) { ids.Add(d.Id); foreach (var s in d.Steps) CollectStep(s, ids); }
    }
    private static void CollectStep(StepNode s, List<string> ids)
    {
        ids.Add(s.Id);
        foreach (var a in s.Assertions ?? Enumerable.Empty<AssertionResult>()) ids.Add(a.Id);
        foreach (var ch in s.Children ?? Enumerable.Empty<StepNode>()) CollectStep(ch, ids);
        foreach (var it in s.Iterations ?? Enumerable.Empty<Iteration>())
        {
            ids.Add(it.Id);
            foreach (var st in it.Steps) CollectStep(st, ids);
        }
    }

    // T050: the same corpus run sequentially and in parallel yields equivalent node sets/outcomes.
    [Fact]
    public async Task Parallel_And_Sequential_ProduceEquivalentResults()
    {
        var caseExecutor = Substitute.For<IJTestCaseExecutor>();
        caseExecutor.ExecuteAsync(Arg.Any<JTestCase>(), Arg.Any<TestExecutionContext>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IEnumerable<JTestCaseResult>>(new[] { new JTestCaseResult { TestCaseName = "c" } }));

        var executor = new JTestSuiteExecutor(
            caseExecutor,
            Substitute.For<IVariablesContext>(),
            Substitute.For<ITemplateContext>(),
            AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) }));

        var suites = new[]
        {
            new JTestSuite { FilePath = "a.json", Tests = new List<JTestCase> { new() { Name = "c" } } },
            new JTestSuite { FilePath = "b.json", Tests = new List<JTestCase> { new() { Name = "c" } } },
            new JTestSuite { FilePath = "c.json", Tests = new List<JTestCase> { new() { Name = "c" } } }
        };

        var sequential = (await executor.Execute(suites)).ToList();
        var parallel = executor.ExecuteParallel(suites, 4).ToList();

        var seqTrace = ExecutionTraceAssembler.Assemble(sequential, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
        var parTrace = ExecutionTraceAssembler.Assemble(parallel, "2.0.0", 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        Assert.Equal(3, seqTrace.Suites.Count);
        Assert.Equal(3, parTrace.Suites.Count); // no suite dropped in the parallel path
        Assert.Equal(seqTrace.Outcome, parTrace.Outcome);

        // Same multiset of (suite name → outcome), order-independent.
        static IEnumerable<(string, Outcome)> Set(ExecutionTrace t) =>
            t.Suites.Select(s => (s.Name, s.Outcome)).OrderBy(x => x.Item1);
        Assert.Equal(Set(seqTrace), Set(parTrace));
    }
}
