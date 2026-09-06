using System.Text.RegularExpressions;
using JTest.Core.Language.Validation;
using Xunit;

namespace JTest.UnitTests.Documentation;

/// <summary>
/// Every full test-definition example embedded in the docs must validate against the shipped
/// language schema, so the docs cannot drift from the implemented system (FR-045 / SC-016).
/// </summary>
public class DocExamplesValidateTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JTest.sln")))
            dir = dir.Parent;
        return dir!.FullName;
    }

    public static IEnumerable<object[]> SuiteExamples()
    {
        var docs = Path.Combine(RepoRoot(), "docs");
        if (!Directory.Exists(docs)) yield break;

        var fenced = new Regex("```json\\s*\\n(.*?)```", RegexOptions.Singleline);
        foreach (var file in Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var index = 0;
            foreach (Match match in fenced.Matches(text))
            {
                index++;
                var json = match.Groups[1].Value;
                // Only full test-definition examples (those with a "tests" array) are validated;
                // partial step/response snippets are illustrative and intentionally skipped.
                if (json.Contains("\"tests\""))
                    yield return new object[] { $"{Path.GetFileName(file)}#{index}", json };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SuiteExamples))]
    public void DocExample_ValidatesAgainstSchema(string id, string json)
    {
        var result = new SchemaValidator().Validate(json);
        Assert.True(result.IsValid,
            $"{id}: {string.Join("; ", result.Diagnostics.Select(d => $"{d.Location}:{d.Message}"))}");
    }
}
