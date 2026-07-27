namespace JTest.Language.Documents;

/// <summary>One dataset row of a data-driven test case.</summary>
/// <param name="Name">Unique name of the row within its case.</param>
/// <param name="Case">The row values exposed as the <c>$.case</c> scope.</param>
public sealed record DatasetDefinition(
    string Name,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Case);
