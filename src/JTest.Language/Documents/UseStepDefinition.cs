namespace JTest.Language.Documents;

/// <summary>A <c>use</c> step: invokes a template with arguments.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">See <see cref="StepDefinition"/>.</param>
/// <param name="Template">Name of the template to invoke.</param>
/// <param name="With">Arguments for the template's declared parameters.</param>
public sealed record UseStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert,
    string Template,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> With)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "use";
}
