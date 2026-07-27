namespace JTest.Language.Documents;

/// <summary>Optional descriptive metadata of a suite.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Description">Longer description.</param>
public sealed record SuiteInfo(string? Name, string? Description);
