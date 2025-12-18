using JTest.Core.Models;
using JTest.Core.Variables;
using System.Text.Json;

namespace JTest.Core.Output.Markdown
{
    public sealed class MarkdownOutputGenerator(IVariablesContext variablesContext) : IOutputGenerator
    {
        private const string lineBreak = "<br/>";
        private readonly MarkdownTestCaseResultWriter testCaseWriter = new();
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        public string FileExtension => ".md";

        public string GenerateOutput(string fileName, string? testSuiteName, string? testSuiteDescription, IEnumerable<JTestCaseResult> results, bool isDebug)
        {
            using var writer = new StringWriter();
            writer.WriteLine($"# Test Results for suite '{testSuiteName ?? fileName}'");
            if (!string.IsNullOrWhiteSpace(testSuiteDescription))
            {
                writer.WriteLine($"Description: {testSuiteDescription}");
            }

            writer.WriteLine(lineBreak);

            if (isDebug && variablesContext.GlobalVariables?.Count > 0)
            {
                writer.WriteLine("### Global variables:");
                WriteVariables(writer, variablesContext.GlobalVariables);
                writer.WriteLine();
            }
            if (isDebug && variablesContext.EnvironmentVariables?.Count > 0)
            {
                writer.WriteLine("### Environment variables:");
                WriteVariables(writer, variablesContext.EnvironmentVariables);
                writer.WriteLine();
            }

            writer.WriteLine();
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
                writer.WriteLine();
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

        static void WriteVariables(TextWriter writer, IReadOnlyDictionary<string, object?> variables)
        {
            var json = JsonSerializer.Serialize(variables, jsonSerializerOptions);
            writer.WriteLine("```json");
            writer.Write(json);
            writer.WriteLine();
            writer.WriteLine("```");

            writer.WriteLine();
        }
    }
}
