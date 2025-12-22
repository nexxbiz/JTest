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