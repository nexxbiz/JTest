using System.Text.Json;

namespace JTest.Language.Tests.TestSupport;

internal sealed record FixtureIndexEntry(
    string File,
    string Kind,
    bool Bundle,
    bool IsValid,
    bool? SchemaValid,
    IReadOnlyList<FixtureExpectation> Expect);

internal sealed record FixtureExpectation(string Code, string Pointer);

internal static class FixtureIndex
{
    internal static string Root { get; } = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    internal static IReadOnlyList<FixtureIndexEntry> Load()
    {
        var json = File.ReadAllText(Path.Combine(Root, "index.json"));
        using var document = JsonDocument.Parse(json);
        var entries = new List<FixtureIndexEntry>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var expectations = new List<FixtureExpectation>();
            foreach (var expected in element.GetProperty("expect").EnumerateArray())
            {
                expectations.Add(new FixtureExpectation(
                    expected.GetProperty("code").GetString()!,
                    expected.GetProperty("pointer").GetString()!));
            }

            entries.Add(new FixtureIndexEntry(
                element.GetProperty("file").GetString()!,
                element.GetProperty("kind").GetString()!,
                element.TryGetProperty("bundle", out var bundle) && bundle.GetBoolean(),
                element.GetProperty("isValid").GetBoolean(),
                element.GetProperty("schemaValid").ValueKind == JsonValueKind.Null
                    ? null
                    : element.GetProperty("schemaValid").GetBoolean(),
                expectations));
        }

        return entries;
    }

    internal static string ReadFixture(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath));

    internal static string ResolveSibling(string relativeToFixture, string reference) =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Path.Combine(Root, relativeToFixture))!,
            reference));
}
