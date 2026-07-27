namespace JTest.Engine.Contexts;

/// <summary>The kind of an execution frame.</summary>
public enum FrameKind
{
    /// <summary>The root frame of one test-case (or dataset) run.</summary>
    Case,

    /// <summary>A loop pass frame; shares its parent's scratch scope.</summary>
    Loop,

    /// <summary>A template invocation frame; isolated scratch scope and names.</summary>
    Template,
}
