using JTest.Core.Debugging;
using JTest.Core.Models;

namespace JTest.Core.Output.Markdown
{
    public sealed class MarkdownOutputGenerator : IOutputGenerator
    {
        private const string lineBreak = "<br/>";
        private readonly MarkdownTestCaseResultWriter testCaseWriter = new();

        public string FileExtension => ".md";

        public string GenerateOutput(string fileName, string? testSuiteName, string? testSuiteDescription, IEnumerable<JTestCaseResult> results, bool isDebug, Dictionary<string, object>? environment, Dictionary<string, object>? globals)
        {
            using var writer = new StringWriter();
            writer.WriteLine($"# Test Results for suite '{testSuiteName ?? fileName}'");
            if (!string.IsNullOrWhiteSpace(testSuiteDescription))
            {
                writer.WriteLine($"Description: {testSuiteDescription}");
            }

            writer.WriteLine(lineBreak);

            if (isDebug && globals?.Count > 0)
            {
                writer.WriteLine("### Global variables:");
                WriteVariables(writer, globals);
                writer.WriteLine();
            }
            if (isDebug && environment?.Count > 0)
            {
                writer.WriteLine("### Environment variables:");
                WriteVariables(writer, environment);
                writer.WriteLine();
            }

            writer.WriteLine("# Test cases");
            writer.WriteLine();
            writer.WriteLine("Total cases executed: " + results.Count() + " <br/>");

            if (results.Any(x => x.Success))
            {
                writer.WriteLine($"Cases passed ({results.Count(x => x.Success)}):");
                foreach (var result in results.Where(x => x.Success))
                {
                    writer.WriteLine($"1. {result.TestCaseName}");
                }
            }

            if (results.Any(x => !x.Success))
            {
                writer.WriteLine($"Cases failed ({results.Count(x => !x.Success)}):");
                foreach (var result in results.Where(x => !x.Success))
                {
                    writer.WriteLine($"1. {result.TestCaseName}");
                }
            }

            writer.WriteLine();

            foreach (var result in results)
            {
                testCaseWriter.Write(writer, result, isDebug);
                writer.WriteLine(lineBreak);
                writer.WriteLine();
            }

            writer.Flush();
            return writer.ToString();
        }

        static void WriteVariables(TextWriter writer, IDictionary<string, object> variables)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(variables, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            writer.WriteLine("```json");
            writer.Write(json);
            writer.WriteLine();
            writer.WriteLine("```");

            writer.WriteLine();
        }
    }
}
