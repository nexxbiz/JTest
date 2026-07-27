using System.Text.Json.Nodes;
using JTest.Language.Diagnostics;

namespace JTest.Engine.Tracing;

/// <summary>
/// One node of the immutable-after-run execution trace: the single source
/// of truth every report is projected from. Every executed (or skipped)
/// unit of work appears here — nothing is overwritten or hidden.
/// </summary>
public sealed class TraceNode
{
    private readonly List<TraceNode> children = [];
    private readonly List<LanguageDiagnostic> diagnostics = [];

    /// <summary>Creates a node.</summary>
    /// <param name="kind">The node kind.</param>
    /// <param name="path">The stable execution path, e.g. <c>suites/0/cases/2/steps/1</c>.</param>
    /// <param name="ordinal">1-based position among siblings of the same collection.</param>
    public TraceNode(TraceNodeKind kind, string path, int ordinal)
    {
        Kind = kind;
        Path = path;
        Ordinal = ordinal;
    }

    /// <summary>The node kind.</summary>
    public TraceNodeKind Kind { get; }

    /// <summary>The stable execution path.</summary>
    public string Path { get; }

    /// <summary>1-based position among siblings.</summary>
    public int Ordinal { get; }

    /// <summary>Zero-based iteration index for iteration nodes.</summary>
    public int? IterationIndex { get; init; }

    /// <summary>The step <c>type</c> for step nodes.</summary>
    public string? StepType { get; init; }

    /// <summary>The step id, when declared.</summary>
    public string? StepId { get; init; }

    /// <summary>Display name: step/case name or suite file.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Template name for template invocation nodes.</summary>
    public string? TemplateName { get; init; }

    /// <summary>Dataset name for dataset-run nodes.</summary>
    public string? DatasetName { get; init; }

    /// <summary>The outcome; defaults to passed until recorded otherwise.</summary>
    public TraceOutcome Outcome { get; private set; }

    /// <summary>Wall-clock start recorded by the engine clock.</summary>
    public DateTimeOffset StartUtc { get; set; }

    /// <summary>Duration recorded by the engine clock.</summary>
    public double DurationMs { get; set; }

    /// <summary>Redacted, kind-specific evidence (http exchange, assertion detail, loop counts).</summary>
    public JsonObject? Evidence { get; set; }

    /// <summary>Diagnostics attached to this node.</summary>
    public IReadOnlyList<LanguageDiagnostic> Diagnostics => diagnostics;

    /// <summary>Child nodes in execution order.</summary>
    public IReadOnlyList<TraceNode> Children => children;

    /// <summary>Appends a child node.</summary>
    /// <param name="child">The child to append.</param>
    public void AddChild(TraceNode child) => children.Add(child);

    /// <summary>Attaches a diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to attach.</param>
    public void AddDiagnostic(LanguageDiagnostic diagnostic) => diagnostics.Add(diagnostic);

    /// <summary>Records this node's own outcome, keeping the strongest state.</summary>
    /// <param name="outcome">The outcome to record.</param>
    public void RecordOutcome(TraceOutcome outcome)
    {
        if (Precedence(outcome) > Precedence(Outcome))
        {
            Outcome = outcome;
        }
    }

    /// <summary>
    /// Recomputes this node's outcome from its children: a node can never
    /// out-claim its weakest child, which structurally rules out the 1.x
    /// false-green class of defects.
    /// </summary>
    public void SealFromChildren()
    {
        foreach (var child in children)
        {
            RecordOutcome(child.Outcome);
        }
    }

    private static int Precedence(TraceOutcome outcome) => outcome switch
    {
        TraceOutcome.Error => 5,
        TraceOutcome.TimedOut => 4,
        TraceOutcome.Cancelled => 3,
        TraceOutcome.Failed => 2,
        TraceOutcome.Skipped => 1,
        _ => 0,
    };
}
