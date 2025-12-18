using JTest.Core.Models;
using JTest.Core.Output;
using Spectre.Console;

namespace JTest.Cli.Utilities
{
    public sealed class TestExecutionResultsProcessor(IAnsiConsole console, IOutputGenerator outputGenerator)
    {
        static string GetOutputDirectory(string? outputDirectory)
        {
            if(!string.IsNullOrWhiteSpace(outputDirectory))
                return outputDirectory;

            return Directory.GetCurrentDirectory();
        }

        internal void ProcessResults(IEnumerable<TestFileExecutionResult> results, string? outputDirectoryPath, bool isDebug, bool skipOutput)
        {
            var outputDirectory = GetOutputDirectory(outputDirectoryPath);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            WriteResultsToConsole(results);

            foreach (var result in results)
            {
                var output = outputGenerator.GenerateOutput(
                    result.FilePath,
                    result.TestSuiteName,
                    result.TestSuiteDescription,
                    result.TestCaseResults,
                    isDebug: isDebug
                );

                if (!skipOutput)
                {
                    var outputFileName = GenerateTestCaseOutputFileName(result.FilePath, result.TestCaseResults.All(x => x.Success));
                    var outputFilePath = Path.Combine(outputDirectory, $"{outputFileName}{outputGenerator.FileExtension}");

                    console.WriteLine($"Writing output report to: {outputFilePath}");
                    File.WriteAllText(outputFilePath, output);
                }
            }
        }

        private void WriteResultsToConsole(IEnumerable<TestFileExecutionResult> results)
        {
            console.WriteLine();
            console.WriteLine($"OVERALL TEST SUMMARY");

            console.WriteLine($"Files processed: {results.Count()}");
            console.WriteLine();
            console.WriteLine($"Files passed:");

            var filesPassed = results.Where(x => x.CasesFailed == 0).Select(x => x.TestSuiteName ?? x.FilePath);
            foreach (var file in filesPassed)
            {
                console.WriteLine("  - " + file);
            }
            console.WriteLine();

            var filesFailed = results.Where(x => x.CasesFailed > 0).Select(x => x.TestSuiteName ?? x.FilePath);
            if (filesFailed.Any())
            {
                console.WriteLine($"Files failed:");
                foreach (var file in filesFailed)
                {
                    console.WriteLine("  - " + file);
                }
                console.WriteLine();
            }

            console.WriteLine($"Total test cases executed: {results.Sum(x => x.TestCaseResults.Count())}");
            console.WriteLine($"Total test cases passed: {results.Sum(x => x.CasesPassed)}");
            console.WriteLine($"Total test cases failed: {results.Sum(x => x.CasesFailed)}");
        }


        private static string GenerateTestCaseOutputFileName(string filePath, bool success)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);

            var dateTimeString = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");

            var safeName = string.Concat(
                name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            );

            var result = $"{safeName}_{dateTimeString}";
            if (success)
            {
                return $"{result}_PASSED";
            }

            return $"{result}_FAILED";
        }
    }
}
