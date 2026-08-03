namespace JTest.Core;

/// <summary>Summary of a validation run. Counts are honest (each file is exactly one of the two).</summary>
public readonly record struct JTestValidationSummary(int Valid, int Invalid)
{
    public int Total => Valid + Invalid;
    public bool HasInvalid => Invalid > 0;
}

public interface IJTestSuiteValidator
{
    Task<JTestValidationSummary> ValidateJTestSuites(IEnumerable<string> testFilePatterns, IEnumerable<string> categories);
}
