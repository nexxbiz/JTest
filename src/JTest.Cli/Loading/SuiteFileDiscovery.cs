using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace JTest.Cli.Loading;

/// <summary>
/// Static deterministic suite-file discovery: glob patterns (with <c>!</c>
/// exclusions) resolved against a base directory, results ordered by
/// ordinal path.
/// </summary>
public static class SuiteFileDiscovery
{
    /// <summary>Resolves the patterns to existing files in deterministic order.</summary>
    /// <param name="baseDirectory">The directory patterns are relative to.</param>
    /// <param name="patterns">Include patterns; a leading <c>!</c> excludes.</param>
    public static IReadOnlyList<string> Resolve(string baseDirectory, IReadOnlyList<string> patterns)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
            {
                matcher.AddExclude(pattern[1..]);
            }
            else if (Path.IsPathRooted(pattern) && File.Exists(pattern))
            {
                // An explicit absolute file path bypasses globbing.
                continue;
            }
            else
            {
                matcher.AddInclude(pattern);
            }
        }

        var results = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var pattern in patterns)
        {
            if (Path.IsPathRooted(pattern) && File.Exists(pattern))
            {
                results.Add(Path.GetFullPath(pattern));
            }
        }

        var root = new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory));
        foreach (var match in matcher.Execute(root).Files)
        {
            results.Add(Path.GetFullPath(Path.Combine(baseDirectory, match.Path)));
        }

        return results.ToList();
    }
}
