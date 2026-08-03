using Xunit;

namespace JTest.UnitTests.Packaging;

/// <summary>
/// Guards release honesty (FR-034/035): the version is single-sourced, the LICENSE exists, and the
/// README's license link resolves. (1.0 shipped csproj 1.0.0 vs tag v1.0.3 and a dead LICENSE link.)
/// </summary>
public class ReleaseMetadataTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JTest.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Fact]
    public void LicenseFile_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "LICENSE")), "LICENSE file must exist.");
    }

    [Fact]
    public void ReadmeLicenseLink_Resolves_AndDeclaresMit()
    {
        var root = RepoRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.Contains("MIT", readme);
        Assert.Contains("(LICENSE)", readme); // markdown link target
        Assert.True(File.Exists(Path.Combine(root, "LICENSE")));
    }

    [Fact]
    public void Version_IsSingleSourced_InDirectoryBuildProps()
    {
        var root = RepoRoot();
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains("<Version>", props);

        // No project may re-declare its own version — that is what drifted in 1.0.
        var core = File.ReadAllText(Path.Combine(root, "src", "JTest.Core", "JTest.Core.csproj"));
        var cli = File.ReadAllText(Path.Combine(root, "src", "JTest.Cli", "JTest.Cli.csproj"));
        Assert.DoesNotContain("<PackageVersion>", core);
        Assert.DoesNotContain("<Version>", core);
        Assert.DoesNotContain("<PackageVersion>", cli);
        Assert.DoesNotContain("<Version>", cli);
    }
}
