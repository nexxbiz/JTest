namespace JTest.Engine.Tracing;

/// <summary>The kind of one execution trace node.</summary>
public enum TraceNodeKind
{
    /// <summary>The whole run.</summary>
    Run,

    /// <summary>One suite file.</summary>
    Suite,

    /// <summary>One test case.</summary>
    Case,

    /// <summary>One dataset run of a data-driven case.</summary>
    DatasetRun,

    /// <summary>One step execution.</summary>
    Step,

    /// <summary>One template invocation.</summary>
    TemplateInvocation,

    /// <summary>One loop iteration (or while pass).</summary>
    Iteration,

    /// <summary>One assertion evaluation.</summary>
    Assertion,
}
