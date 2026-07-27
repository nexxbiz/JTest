namespace JTest.Language.Documents;

/// <summary>A <c>wait</c> step: delays execution.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">See <see cref="StepDefinition"/>.</param>
/// <param name="Ms">Delay in milliseconds: a non-negative number or an expression string.</param>
public sealed record WaitStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert,
    System.Text.Json.JsonElement Ms)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "wait";
}
