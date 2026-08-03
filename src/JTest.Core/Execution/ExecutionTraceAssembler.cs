using System.Reflection;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Tracing;
using JTest.Core.TypeDescriptors;
using OldAssertion = JTest.Core.Assertions.AssertionResult;

namespace JTest.Core.Execution;

/// <summary>
/// Assembles the canonical <see cref="ExecutionTrace"/> — the run's evidence — from executed
/// suite/case/step results. It is iteration-aware (loops contribute real <see cref="Iteration"/>
/// nodes from the retained per-pass history) and captures crashing suites as errored nodes.
/// This is the execution layer's trace producer; as the interim result types
/// (<c>StepProcessedResult</c>/<c>JTestCaseResult</c>) are retired, trace construction moves into
/// the step/case executors and this assembler shrinks. It is NOT a compatibility shim over a frozen
/// legacy model.
/// </summary>
public static class ExecutionTraceAssembler
{
    public static ExecutionTrace Assemble(
        IReadOnlyList<JTestSuiteExecutionResult> results,
        string toolVersion,
        int exitCode,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        var suites = results.Select(Suite).ToList();
        return new ExecutionTrace
        {
            ToolVersion = toolVersion,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMs = (endedAt - startedAt).TotalMilliseconds,
            Outcome = OutcomeExtensions.Aggregate(suites.Select(s => s.Outcome)),
            ExitCode = exitCode,
            Counts = Rollup.From(suites.Select(s => s.Outcome)),
            Suites = suites
        };
    }

    private static SuiteResult Suite(JTestSuiteExecutionResult r, int index)
    {
        var path = TraceBuilder.Path("", "suite", index);

        if (r.Errored)
        {
            return new SuiteResult
            {
                Id = path, Path = path, Name = r.TestSuiteName ?? r.FilePath, FilePath = r.FilePath,
                Outcome = Outcome.Errored, Counts = Rollup.From(new[] { Outcome.Errored }),
                Cases = Array.Empty<CaseResult>(),
                Diagnostics = new[] { Diagnostic.Error(r.ExecutionError!) }
            };
        }

        var cases = r.TestCaseResults.Select((c, i) => Case(c, path, i)).ToList();
        return new SuiteResult
        {
            Id = path, Path = path, Name = r.TestSuiteName ?? r.FilePath,
            Description = r.TestSuiteDescription, FilePath = r.FilePath,
            Outcome = OutcomeExtensions.Aggregate(cases.Select(c => c.Outcome)),
            Counts = Rollup.From(cases.Select(c => c.Outcome)),
            Cases = cases
        };
    }

    private static CaseResult Case(JTestCaseResult c, string parent, int index)
    {
        var path = TraceBuilder.Path(parent, "case", index);
        var datasetPath = TraceBuilder.Path(path, "dataset", 0);
        var steps = c.StepResults.Select((s, i) => Step(s, datasetPath, i)).ToList();
        var outcome = c.Success ? Outcome.Passed : Outcome.Failed;

        var dataset = new DatasetResult
        {
            Id = datasetPath, Path = datasetPath, Label = c.Dataset?.Name ?? "default",
            Outcome = outcome, Counts = Rollup.From(steps.Select(s => s.Outcome)), DurationMs = c.DurationMs,
            Steps = steps,
            Diagnostics = c.ErrorMessage is null ? null : new[] { Diagnostic.Error(c.ErrorMessage) }
        };

        return new CaseResult
        {
            Id = path, Path = path, Name = c.TestCaseName, Outcome = outcome,
            Counts = Rollup.From(new[] { outcome }), DurationMs = c.DurationMs,
            Datasets = new[] { dataset }
        };
    }

    private static StepNode Step(StepProcessedResult s, string parent, int index)
    {
        var ordinal = index + 1;
        var path = TraceBuilder.Path(parent, "step", ordinal);
        var outcome = s.Success ? Outcome.Passed : Outcome.Failed;

        var iterations = s.Iterations.Select(it => Iteration(it, path)).ToList();
        var isLoop = iterations.Count > 0;

        var children = isLoop
            ? new List<StepNode>()
            : s.InnerResults.Select((inner, i) => Step(inner, path, i)).ToList();

        var kind = isLoop ? NodeKind.Loop : (children.Count > 0 ? NodeKind.Template : NodeKind.Step);

        var assertions = s.AssertionResults
            .Select((a, i) => Assertion(a, TraceBuilder.Path(path, "assert", i + 1)))
            .ToList();

        return new StepNode
        {
            Id = path, Path = path, Kind = kind,
            StepType = LanguageType(s.Step), Ordinal = ordinal,
            Name = s.Step.Configuration?.Name, Description = s.Step.Configuration?.Description,
            DurationMs = s.DurationMs, Outcome = outcome,
            Assertions = assertions.Count > 0 ? assertions : null,
            Children = children.Count > 0 ? children : null,
            Iterations = isLoop ? iterations : null,
            Diagnostics = string.IsNullOrEmpty(s.ErrorMessage) ? null : new[] { Diagnostic.Error(s.ErrorMessage!) }
        };
    }

    /// <summary>The language type discriminator ("for", "while", "http", …): the step's
    /// <see cref="TypeIdentifierAttribute"/> when present, else its conventional type name.</summary>
    private static string LanguageType(IStep step) =>
        step.GetType().GetCustomAttribute<TypeIdentifierAttribute>()?.Id ?? step.TypeName;

    private static Iteration Iteration(StepIteration it, string loopPath)
    {
        var path = TraceBuilder.Path(loopPath, "iteration", it.Index);
        var steps = it.Steps.Select((s, i) => Step(s, path, i)).ToList();
        return new Iteration
        {
            Id = path, Path = path, Index = it.Index,
            Outcome = it.Success ? Outcome.Passed : Outcome.Failed,
            Steps = steps
        };
    }

    private static AssertionResult Assertion(OldAssertion a, string id) => new()
    {
        Id = id,
        Operation = string.IsNullOrEmpty(a.Operation) ? "assert" : a.Operation,
        Expected = a.ExpectedValue,
        Actual = a.ActualValue,
        Outcome = a.Success ? Outcome.Passed : Outcome.Failed,
        Message = string.IsNullOrEmpty(a.ErrorMessage) ? null : a.ErrorMessage
    };
}
