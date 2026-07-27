namespace JTest.Language.Documents;

/// <summary>One reusable template.</summary>
/// <param name="Name">Unique template name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Parameters">Declared parameters by name.</param>
/// <param name="Steps">Steps executed on invocation; never empty.</param>
/// <param name="Output">Values exported to the caller: output name to expression.</param>
public sealed record TemplateDefinition(
    string Name,
    string? Description,
    IReadOnlyDictionary<string, TemplateParameterDefinition> Parameters,
    IReadOnlyList<StepDefinition> Steps,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Output);
