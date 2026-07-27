using System.Text.RegularExpressions;
using JTest.AcceptanceTests.TestSupport;

namespace JTest.AcceptanceTests;

/// <summary>
/// Proves the Program Kit consumption boundary: only locally prepared
/// NuGet packages through the cleared-source mapped feed — never a
/// project reference, source inclusion, file reference, or assembly hint
/// path into the Program Kit repository.
/// </summary>
[TestClass]
public sealed partial class ProgramKitConsumptionBoundaryTests
{
    [GeneratedRegex("""<(ProjectReference|Reference|Compile|Content|None|Analyzer)\b[^>]*(Include|HintPath)\s*=\s*"([^"]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ProjectItems();

    private static IEnumerable<string> RepositoryProjects() =>
        Directory
            .EnumerateFiles(CliWorkspace.RepoRoot, "*.*proj", SearchOption.AllDirectories)
            .Where(static path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [TestMethod]
    public void NoProjectReachesIntoTheProgramKitRepository()
    {
        var failures = new List<string>();
        foreach (var project in RepositoryProjects())
        {
            var projectDirectory = Path.GetDirectoryName(project)!;
            foreach (Match match in ProjectItems().Matches(File.ReadAllText(project)))
            {
                var include = match.Groups[3].Value;
                if (include.Contains("program-kit", StringComparison.OrdinalIgnoreCase) ||
                    include.Contains("Orbyss.ProgramKit", StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{Path.GetFileName(project)}: {match.Groups[1].Value} -> {include}");
                    continue;
                }

                // Any path-based item must stay inside this repository.
                if (include.Contains("..", StringComparison.Ordinal))
                {
                    var resolved = Path.GetFullPath(Path.Combine(projectDirectory, include));
                    if (!resolved.StartsWith(CliWorkspace.RepoRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"{Path.GetFileName(project)}: {match.Groups[1].Value} escapes the repository -> {include}");
                    }
                }
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
    }

    [TestMethod]
    public void NuGetConfigurationClearsSourcesAndMapsProgramKitToTheLocalFeed()
    {
        var configuration = File.ReadAllText(Path.Combine(CliWorkspace.RepoRoot, "NuGet.Config"));
        StringAssert.Contains(configuration, "<clear />");
        StringAssert.Contains(configuration, "value=\"packages/local-feed\"");
        StringAssert.Contains(configuration, "<package pattern=\"Orbyss.ProgramKit.*\" />");
    }

    [TestMethod]
    public void TheLocalFeedManifestPinsAnExactCleanProgramKitCommit()
    {
        var manifest = File.ReadAllText(
            Path.Combine(CliWorkspace.RepoRoot, "packages", "local-feed.manifest.json"));
        StringAssert.Contains(manifest, "\"programKitCommit\"");
        StringAssert.Contains(manifest, "\"programKitDirty\":  false");
        StringAssert.Contains(manifest, "sha256:");
    }
}
