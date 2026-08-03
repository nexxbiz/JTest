using System.Reflection;
using System.Text.Json;
using JTest.Core.Models;
using JTest.Core.Security;
using JTest.Core.Steps;
using JTest.Core.Tracing;
using JTest.Core.TypeDescriptors;
using OldAssertion = JTest.Core.Assertions.AssertionResult;

namespace JTest.Core.Execution;

/// <summary>
/// Assembles the canonical <see cref="ExecutionTrace"/> — the run's evidence — from executed
/// suite/case/step results. It is iteration-aware (loops contribute real <see cref="Iteration"/>
/// nodes from the retained per-pass history), captures crashing suites as errored nodes, and
/// redacts secrets/cookies/authorization by value everywhere they appear (FR-025/026/042). This is
/// the execution layer's trace producer; as the interim result types are retired, trace
/// construction moves into the step/case executors and this assembler shrinks. It is NOT a
/// compatibility shim over a frozen legacy model.
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
        // One redactor for the whole run: register sensitive values first so they are masked
        // wherever they appear (a token seen in a header is also masked in any body).
        var redactor = new ValueRedactor();
        RegisterSecrets(results, redactor);

        var suites = results.Select((r, i) => Suite(r, i, redactor)).ToList();
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

    private static SuiteResult Suite(JTestSuiteExecutionResult r, int index, ValueRedactor redactor)
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

        var cases = r.TestCaseResults.Select((c, i) => Case(c, path, i, redactor)).ToList();
        return new SuiteResult
        {
            Id = path, Path = path, Name = r.TestSuiteName ?? r.FilePath,
            Description = r.TestSuiteDescription, FilePath = r.FilePath,
            Outcome = OutcomeExtensions.Aggregate(cases.Select(c => c.Outcome)),
            Counts = Rollup.From(cases.Select(c => c.Outcome)),
            Cases = cases
        };
    }

    private static CaseResult Case(JTestCaseResult c, string parent, int index, ValueRedactor redactor)
    {
        var path = TraceBuilder.Path(parent, "case", index);
        var datasetPath = TraceBuilder.Path(path, "dataset", 0);
        var steps = c.StepResults.Select((s, i) => Step(s, datasetPath, i, redactor)).ToList();

        // Aggregate the outcome from the steps so distinct outcomes (timed-out, cancelled) propagate
        // up; fall back to the case flag when there are no steps, and never mask a case-level error.
        var outcome = steps.Count > 0
            ? OutcomeExtensions.Aggregate(steps.Select(s => s.Outcome))
            : (c.Success ? Outcome.Passed : Outcome.Failed);
        if (outcome == Outcome.Passed && !c.Success) outcome = Outcome.Failed;

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

    private static StepNode Step(StepProcessedResult s, string parent, int index, ValueRedactor redactor)
    {
        var ordinal = index + 1;
        var path = TraceBuilder.Path(parent, "step", ordinal);
        var outcome = s.TimedOut ? Outcome.TimedOut
            : s.Cancelled ? Outcome.Cancelled
            : s.Success ? Outcome.Passed : Outcome.Failed;

        var iterations = s.Iterations.Select(it => Iteration(it, path, redactor)).ToList();
        var isLoop = iterations.Count > 0;

        var children = isLoop
            ? new List<StepNode>()
            : s.InnerResults.Select((inner, i) => Step(inner, path, i, redactor)).ToList();

        var kind = isLoop ? NodeKind.Loop : (children.Count > 0 ? NodeKind.Template : NodeKind.Step);

        var assertions = s.AssertionResults
            .Select((a, i) => Assertion(a, TraceBuilder.Path(path, "assert", i + 1), redactor))
            .ToList();

        return new StepNode
        {
            Id = path, Path = path, Kind = kind,
            StepType = LanguageType(s.Step), Ordinal = ordinal,
            Name = s.Step.Configuration?.Name, Description = s.Step.Configuration?.Description,
            DurationMs = s.DurationMs, Outcome = outcome,
            Http = HttpFrom(s, redactor),
            Assertions = assertions.Count > 0 ? assertions : null,
            Children = children.Count > 0 ? children : null,
            Iterations = isLoop ? iterations : null,
            Diagnostics = string.IsNullOrEmpty(s.ErrorMessage) ? null : new[] { Diagnostic.Error(s.ErrorMessage!) }
        };
    }

    private static Iteration Iteration(StepIteration it, string loopPath, ValueRedactor redactor)
    {
        var path = TraceBuilder.Path(loopPath, "iteration", it.Index);
        var steps = it.Steps.Select((s, i) => Step(s, path, i, redactor)).ToList();
        return new Iteration
        {
            Id = path, Path = path, Index = it.Index,
            Outcome = it.Success ? Outcome.Passed : Outcome.Failed,
            Steps = steps
        };
    }

    private static AssertionResult Assertion(OldAssertion a, string id, ValueRedactor redactor) => new()
    {
        Id = id,
        Operation = string.IsNullOrEmpty(a.Operation) ? "assert" : a.Operation,
        Expected = RedactScalar(a.ExpectedValue, redactor),
        Actual = RedactScalar(a.ActualValue, redactor),
        Outcome = a.Success ? Outcome.Passed : Outcome.Failed,
        Message = a.ErrorMessage is { Length: > 0 } m ? redactor.Redact(m) : null
    };

    // ---- HTTP exchange projection + redaction -------------------------------------------------

    private static HttpExchange? HttpFrom(StepProcessedResult s, ValueRedactor redactor)
    {
        if (LanguageType(s.Step) != "http" || s.Data is not IReadOnlyDictionary<string, object?> data)
            return null;

        var request = Get(data, "request") as IReadOnlyDictionary<string, object?>;

        return new HttpExchange
        {
            Method = request is null ? null : Get(request, "method")?.ToString(),
            Url = request is null ? null : Get(request, "url")?.ToString(),
            RequestHeaders = RedactHeaders(request is null ? null : Get(request, "headers"), redactor),
            RequestBody = request is null ? null : redactor.Redact(StringifyBody(Get(request, "body"))),
            StatusCode = ToInt(Get(data, "statusCode")),
            Status = ToInt(Get(data, "status")),
            ResponseHeaders = RedactHeaders(Get(data, "headers"), redactor),
            ResponseBody = redactor.Redact(StringifyBody(Get(data, "body")))
        };
    }

    /// <summary>Register secret-like header values from every HTTP step so they are masked everywhere.</summary>
    private static void RegisterSecrets(IReadOnlyList<JTestSuiteExecutionResult> results, ValueRedactor redactor)
    {
        foreach (var suite in results)
            foreach (var @case in suite.TestCaseResults)
                foreach (var step in @case.StepResults)
                    RegisterStepSecrets(step, redactor);
    }

    private static void RegisterStepSecrets(StepProcessedResult s, ValueRedactor redactor)
    {
        if (LanguageType(s.Step) == "http" && s.Data is IReadOnlyDictionary<string, object?> data)
        {
            RegisterHeaderSecrets(Get(data, "headers"), redactor);
            if (Get(data, "request") is IReadOnlyDictionary<string, object?> request)
                RegisterHeaderSecrets(Get(request, "headers"), redactor);
        }

        foreach (var inner in s.InnerResults) RegisterStepSecrets(inner, redactor);
        foreach (var it in s.Iterations)
            foreach (var inner in it.Steps) RegisterStepSecrets(inner, redactor);
    }

    private static void RegisterHeaderSecrets(object? headers, ValueRedactor redactor)
    {
        if (headers is not IReadOnlyDictionary<string, object?> map) return;
        foreach (var (key, value) in map)
        {
            if (!ValueRedactor.IsSecretKey(key)) continue;
            foreach (var v in AsValues(value)) redactor.RegisterSecret(v);
        }
    }

    private static Dictionary<string, object?>? RedactHeaders(object? headers, ValueRedactor redactor)
    {
        if (headers is not IReadOnlyDictionary<string, object?> map) return null;

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map)
        {
            if (ValueRedactor.IsSecretKey(key))
            {
                result[key] = ValueRedactor.Mask;
            }
            else if (value is object?[] arr)
            {
                result[key] = arr.Select(v => (object?)redactor.Redact(v?.ToString())).ToArray();
            }
            else
            {
                result[key] = redactor.Redact(value?.ToString());
            }
        }
        return result;
    }

    private static IEnumerable<string?> AsValues(object? value) => value switch
    {
        object?[] arr => arr.Select(v => v?.ToString()),
        _ => new[] { value?.ToString() }
    };

    private static string? StringifyBody(object? body) => body switch
    {
        null => null,
        string s => s,
        _ => JsonSerializer.Serialize(body)
    };

    private static object? RedactScalar(object? value, ValueRedactor redactor) =>
        value is string s ? redactor.Redact(s) : value;

    private static object? Get(IReadOnlyDictionary<string, object?> d, string key) =>
        d.TryGetValue(key, out var v) ? v : null;

    private static int? ToInt(object? v) => v switch
    {
        int i => i,
        long l => (int)l,
        _ => int.TryParse(v?.ToString(), out var parsed) ? parsed : null
    };

    /// <summary>The language type discriminator ("for", "while", "http", …): the step's
    /// <see cref="TypeIdentifierAttribute"/> when present, else its conventional type name.</summary>
    private static string LanguageType(IStep step) =>
        step.GetType().GetCustomAttribute<TypeIdentifierAttribute>()?.Id ?? step.TypeName;
}
