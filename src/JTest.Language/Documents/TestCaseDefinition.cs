namespace JTest.Language.Documents;

/// <summary>One test case of a suite.</summary>
/// <param name="Name">Unique display name of the case.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Steps">The steps, in execution order; never empty.</param>
/// <param name="Datasets">Optional dataset rows; when present the case runs once per row.</param>
public sealed record TestCaseDefinition(
    string Name,
    string? Description,
    IReadOnlyList<StepDefinition> Steps,
    IReadOnlyList<DatasetDefinition> Datasets);
