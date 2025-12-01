using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Data;
using System.Text.Json;

namespace JTest.Core.Utilities
{
    public static class TestFileSearcher
    {
        public static string[] Search(IEnumerable<string> testFilePatterns, IEnumerable<string> categories)
        {
            var files = Search(testFilePatterns);
            var result = files.Where(filePath => DoesTestFileMatchCategories(filePath, categories));

            return [.. result];
        }

        public static string[] Search(IEnumerable<string> testFilePatterns) => [.. ExpandWildCardPatterns(testFilePatterns)];

        /// <summary>
        /// Expands DevOps-style include/exclude wildcard patterns into file paths.
        /// Supports **, *, ?, and !exclude patterns.
        /// </summary>
        static IEnumerable<string> ExpandWildCardPatterns(IEnumerable<string> patterns)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith('!'))
                {
                    matcher.AddExclude(pattern.Substring(1));  // strip "!"
                }
                else
                {
                    matcher.AddInclude(pattern);
                }
            }

            // Use the working directory as base (adjust if needed)
            var workingDirectory = Directory.GetCurrentDirectory();
            var directoryWrapper = new DirectoryInfoWrapper(
                new DirectoryInfo(workingDirectory)
            );

            var result = matcher.Execute(directoryWrapper);

            return result.Files
                .Select(f => Path.GetFullPath(f.Path))
                .Where(f => f.EndsWith(".json"))
                .OrderBy(f => f);
        }
        
        static bool DoesTestFileMatchCategories(string filePath, IEnumerable<string> categories)
        {
            if (!categories.Any())
            {
                return true;
            }

            var jsonDocument = JsonDocument.Parse(File.ReadAllText(filePath));
            var jsonElement = jsonDocument.RootElement;
            if (!jsonElement.TryGetProperty("categories", out JsonElement categoriesElement) || categoriesElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var testFileCategories = categoriesElement
                .EnumerateArray()
                .Select(x => x.GetString());

            return testFileCategories.Any(testFileCategory =>
            {
                return categories.Contains(testFileCategory, StringComparer.OrdinalIgnoreCase);
            });
        }
    }
}
