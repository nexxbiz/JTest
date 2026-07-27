namespace JTest.Language.Documents;

/// <summary>A validated JTest 2.0 template file.</summary>
/// <param name="LanguageVersion">The exact <c>jtest</c> discriminator value.</param>
/// <param name="Templates">The templates declared by the file.</param>
public sealed record TemplateFileDocument(
    string LanguageVersion,
    IReadOnlyList<TemplateDefinition> Templates);
