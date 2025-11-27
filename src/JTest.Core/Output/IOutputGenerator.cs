using JTest.Core.Models;

namespace JTest.Core.Output
{
    public interface IOutputGenerator
    {
        string GenerateOutput(string fileName, string? testSuiteName, string? testSuiteDescription, IEnumerable<JTestCaseResult> results, bool isDebug, Dictionary<string, object>? environment, Dictionary<string, object>? globals);

        string FileExtension { get; }
    }
}
