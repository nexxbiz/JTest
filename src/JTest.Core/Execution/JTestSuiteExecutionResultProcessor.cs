using JTest.Core.Models;
using Spectre.Console;

namespace JTest.Core.Execution;

/// <summary>
/// Prints the run's console summary. It no longer writes any files: the HTML report and the canonical
/// trace JSON are projections of the trace, written by the run command (see RunCommand.WriteOutputs).
/// The legacy per-suite Markdown writer (HTML-table soup dumped next to the suite) has been retired.
/// </summary>
public sealed class JTestSuiteExecutionResultProcessor(IAnsiConsole console)
    : IJTestSuiteExecutionResultProcessor
{
    public void WriteConsoleSummary(IEnumerable<JTestSuiteExecutionResult> results)
    {
        var list = results as IReadOnlyList<JTestSuiteExecutionResult> ?? results.ToList();

        console.WriteLine();
        console.WriteLine("OVERALL TEST SUMMARY");
        console.WriteLine($"Files processed: {list.Count}");
        console.WriteLine();

        console.WriteLine("Files passed:");
        foreach (var file in list.Where(x => x.CasesFailed == 0).Select(x => x.TestSuiteName ?? x.FilePath))
            console.WriteLine("  - " + file);
        console.WriteLine();

        var filesFailed = list.Where(x => x.CasesFailed > 0).Select(x => x.TestSuiteName ?? x.FilePath).ToList();
        if (filesFailed.Count > 0)
        {
            console.WriteLine("Files failed:");
            foreach (var file in filesFailed)
                console.WriteLine("  - " + file);
            console.WriteLine();
        }

        console.WriteLine($"Total test cases executed: {list.Sum(x => x.TestCaseResults.Count())}");
        console.WriteLine($"Total test cases passed: {list.Sum(x => x.CasesPassed)}");
        console.WriteLine($"Total test cases failed: {list.Sum(x => x.CasesFailed)}");
    }
}
