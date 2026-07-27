using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JTest.AcceptanceTests.TestSupport;
using JTest.Language;

namespace JTest.AcceptanceTests;

/// <summary>
/// Keeps the prose honest: every relative link resolves, and the language
/// reference stays in lockstep with the machine-readable manifest.
/// </summary>
[TestClass]
public sealed partial class DocsIntegrityTests
{
    [GeneratedRegex(@"\]\((?!https?://|#)([^)#]+)")]
    private static partial Regex RelativeLinks();

    [TestMethod]
    public void EveryRelativeDocumentationLinkResolves()
    {
        var failures = new List<string>();
        var documents = Directory
            .EnumerateFiles(Path.Combine(CliWorkspace.RepoRoot, "docs"), "*.md", SearchOption.AllDirectories)
            .Append(Path.Combine(CliWorkspace.RepoRoot, "README.md"))
            .Append(Path.Combine(CliWorkspace.RepoRoot, "CHANGELOG.md"));

        foreach (var document in documents)
        {
            var baseDirectory = Path.GetDirectoryName(document)!;
            foreach (Match match in RelativeLinks().Matches(File.ReadAllText(document)))
            {
                var target = Path.GetFullPath(Path.Combine(baseDirectory, match.Groups[1].Value));
                if (!File.Exists(target) && !Directory.Exists(target))
                {
                    failures.Add($"{Path.GetFileName(document)} -> {match.Groups[1].Value}");
                }
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
    }

    [TestMethod]
    public void LanguageReferenceCoversEveryManifestStepAndOperator()
    {
        var reference = File.ReadAllText(
            Path.Combine(CliWorkspace.RepoRoot, "docs", "language", "reference.md"));
        var manifest = JsonNode.Parse(LanguageContract.LanguageManifestJson)!.AsObject();
        var missing = new List<string>();

        foreach (var step in manifest["steps"]!.AsArray())
        {
            var type = step!["type"]!.GetValue<string>();
            if (!reference.Contains($"`{type}`", StringComparison.Ordinal))
            {
                missing.Add($"step {type}");
            }
        }

        foreach (var assertion in manifest["assertions"]!.AsArray())
        {
            var op = assertion!["op"]!.GetValue<string>();
            if (!reference.Contains($"`{op}`", StringComparison.Ordinal))
            {
                missing.Add($"operator {op}");
            }
        }

        Assert.AreEqual(0, missing.Count, $"reference.md is missing: {string.Join(", ", missing)}");
    }
}
