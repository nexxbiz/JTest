namespace JTest.Core.Tracing;

/// <summary>The kind of a trace node. Serialized camelCase to match the trace schema.</summary>
public enum NodeKind
{
    Run,
    Suite,
    Case,
    Dataset,
    Step,
    Template,
    Loop,
    Iteration,
    Assertion
}

/// <summary>
/// Canonical, versioned execution trace — the single source of truth for a run (Principle I).
/// Property names serialize to camelCase and match contracts/execution-trace.schema.json.
/// </summary>
public sealed record ExecutionTrace
{
    public string TraceSchemaVersion { get; init; } = "1.0.0";
    public required string ToolVersion { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset EndedAt { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public required int ExitCode { get; init; }
    public required Rollup Counts { get; init; }
    public IReadOnlyList<SuiteResult> Suites { get; init; } = Array.Empty<SuiteResult>();
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }

    /// <summary>Opt-in, masked environment/global dump (FR-027/28). Omitted by default.</summary>
    public IReadOnlyDictionary<string, object?>? Environment { get; init; }
}

public sealed record SuiteResult
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public NodeKind Kind { get; init; } = NodeKind.Suite;
    public string? FilePath { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public required Rollup Counts { get; init; }
    public IReadOnlyList<CaseResult> Cases { get; init; } = Array.Empty<CaseResult>();
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }
}

public sealed record CaseResult
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public NodeKind Kind { get; init; } = NodeKind.Case;
    public required string Name { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public required Rollup Counts { get; init; }
    public IReadOnlyList<DatasetResult> Datasets { get; init; } = Array.Empty<DatasetResult>();
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }
}

public sealed record DatasetResult
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public NodeKind Kind { get; init; } = NodeKind.Dataset;
    public string? Label { get; init; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public required Rollup Counts { get; init; }
    public IReadOnlyList<StepNode> Steps { get; init; } = Array.Empty<StepNode>();
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }
}

/// <summary>A single step execution — covers ordinary steps, template expansions, and loops.</summary>
public sealed record StepNode
{
    public required string Id { get; init; }
    public required string Path { get; init; }

    /// <summary>step | template | loop.</summary>
    public NodeKind Kind { get; init; } = NodeKind.Step;

    public required string StepType { get; init; }
    public required int Ordinal { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public HttpExchange? Http { get; init; }
    public ContextChanges? ContextChanges { get; init; }
    public IReadOnlyList<AssertionResult>? Assertions { get; init; }

    /// <summary>Template-expanded child steps.</summary>
    public IReadOnlyList<StepNode>? Children { get; init; }

    /// <summary>Loop iterations (present when <see cref="Kind"/> is <see cref="NodeKind.Loop"/>).</summary>
    public IReadOnlyList<Iteration>? Iterations { get; init; }

    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }
}

public sealed record Iteration
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public NodeKind Kind { get; init; } = NodeKind.Iteration;
    public required int Index { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public double? DurationMs { get; init; }
    public required Outcome Outcome { get; init; }
    public IReadOnlyList<StepNode> Steps { get; init; } = Array.Empty<StepNode>();
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }
}

public sealed record AssertionResult
{
    public required string Id { get; init; }
    public NodeKind Kind { get; init; } = NodeKind.Assertion;
    public required string Operation { get; init; }
    public object? Expected { get; init; }
    public object? Actual { get; init; }

    /// <summary>Only Passed or Failed for an assertion.</summary>
    public required Outcome Outcome { get; init; }
    public string? Message { get; init; }
}

/// <summary>Added/modified context variables for a step (redacted).</summary>
public sealed record ContextChanges
{
    public IReadOnlyDictionary<string, object?>? Added { get; init; }
    public IReadOnlyDictionary<string, object?>? Modified { get; init; }
}

/// <summary>
/// A captured HTTP request/response. Header values are a string or an array of strings
/// (multi-valued, e.g. set-cookie). Cookie/authorization values are redacted (FR-042).
/// </summary>
public sealed record HttpExchange
{
    public string? Method { get; init; }
    public string? Url { get; init; }
    public IReadOnlyDictionary<string, object?>? RequestHeaders { get; init; }
    public string? RequestBody { get; init; }

    /// <summary>Canonical HTTP status code.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Alias of <see cref="StatusCode"/> (retained for compatibility).</summary>
    public int? Status { get; init; }

    public IReadOnlyDictionary<string, object?>? ResponseHeaders { get; init; }
    public string? ResponseBody { get; init; }
    public double? DurationMs { get; init; }
}
