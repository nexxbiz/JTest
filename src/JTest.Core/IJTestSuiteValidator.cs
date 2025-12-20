namespace JTest.Core;

public interface IJTestSuiteValidator
{
    Task ValidateJTestSuites(IEnumerable<string> testFilePatterns, IEnumerable<string> categories);
}