namespace JTest.Language.Documents;

/// <summary>One declared template parameter.</summary>
/// <param name="Type">Declared value type: string, number, boolean, object, or array.</param>
/// <param name="Required">Whether an argument is mandatory.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Default">Optional default used when no argument is supplied.</param>
public sealed record TemplateParameterDefinition(
    string Type,
    bool Required,
    string? Description,
    System.Text.Json.JsonElement? Default);
