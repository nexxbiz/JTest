namespace JTest.Language.Documents;

/// <summary>An <c>assert</c> step: evaluates assertions only.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">The assertions; never empty.</param>
public sealed record AssertStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "assert";
}
