using System.Text.RegularExpressions;

namespace JTest.UnitTests.Fixtures;

/// <summary>
/// Golden-file comparison helper for trace/report tests. Normalizes volatile content
/// (line endings, trailing whitespace, timestamps, durations, and generated ids) so
/// deterministic projections can be pinned. Set the JTEST_UPDATE_GOLDEN environment
/// variable to "1" to (re)write golden files instead of asserting.
/// </summary>
public static class GoldenFile
{
    private static readonly Regex Timestamp =
        new(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z", RegexOptions.Compiled);
    private static readonly Regex DurationMs =
        new(@"""durationMs""\s*:\s*[0-9]+(\.[0-9]+)?", RegexOptions.Compiled);

    /// <summary>Normalize volatile substrings so comparisons are stable across runs.</summary>
    public static string Normalize(string content)
    {
        var text = content.Replace("\r\n", "\n").TrimEnd();
        text = Timestamp.Replace(text, "<TIMESTAMP>");
        text = DurationMs.Replace(text, "\"durationMs\":<DURATION>");
        return text;
    }

    /// <summary>
    /// Assert <paramref name="actual"/> matches the golden file at <paramref name="goldenPath"/>.
    /// When JTEST_UPDATE_GOLDEN=1, writes the normalized actual and passes.
    /// </summary>
    public static void Assert(string goldenPath, string actual)
    {
        var normalized = Normalize(actual);

        if (Environment.GetEnvironmentVariable("JTEST_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
            File.WriteAllText(goldenPath, normalized);
            return;
        }

        if (!File.Exists(goldenPath))
        {
            throw new Xunit.Sdk.XunitException(
                $"Golden file '{goldenPath}' does not exist. Run with JTEST_UPDATE_GOLDEN=1 to create it.");
        }

        var expected = Normalize(File.ReadAllText(goldenPath));
        Xunit.Assert.Equal(expected, normalized);
    }
}
