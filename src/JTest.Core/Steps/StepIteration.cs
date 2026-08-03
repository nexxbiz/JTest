namespace JTest.Core.Steps;

/// <summary>
/// One pass of a loop: its own inner step results and outcome (FR-013). Loops retain every
/// iteration so the trace and report show the complete history — no iteration overwrites another.
/// </summary>
public sealed class StepIteration(int index, bool success, IReadOnlyList<StepProcessedResult> steps)
{
    /// <summary>0-based iteration number.</summary>
    public int Index { get; } = index;

    public bool Success { get; } = success;

    /// <summary>This iteration's own inner step results.</summary>
    public IReadOnlyList<StepProcessedResult> Steps { get; } = steps;
}
