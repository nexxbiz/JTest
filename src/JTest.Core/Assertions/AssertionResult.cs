namespace JTest.Core.Assertions;

/// <summary>
/// Result of an assertion operation
/// </summary>
public sealed record AssertionResult(bool Success, string ErrorMessage = "")
{
    private bool? mask;
    public string Operation { get; init; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public object? ActualValue { get; init; }

    public object? ExpectedValue { get; init; }

    /// <summary>
    /// The original (pre-resolution) actual expression being asserted — e.g. the JSONPath token like
    /// "{{$.this.body.id}}". Surfaced in the report so a reader can see WHAT was asserted, not only the
    /// resolved actual value.
    /// </summary>
    public object? Subject { get; init; }

    /// <summary>
    /// JSONPaths in this assertion's actual/expected that matched nothing. Recorded so the trace and
    /// report can show an unresolved path as its own diagnostic instead of letting it collapse into a
    /// blank value that reads like a data problem (FR-049).
    /// </summary>
    public IReadOnlyList<string> UnresolvedPaths { get; init; } = [];

    public bool MaskValue => mask == true;

    public void SetMask(bool? value)
    {
        if (mask.HasValue)
        {
            throw new InvalidOperationException("Mask is already set.");
        }

        mask = value;
    }
}