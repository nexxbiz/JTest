namespace JTest.Language.Documents;

/// <summary>A <c>while</c> step: repeats child steps until a condition stops holding or a timeout elapses.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">See <see cref="StepDefinition"/>.</param>
/// <param name="Condition">The continue condition, evaluated after each pass.</param>
/// <param name="TimeoutMs">Mandatory overall timeout in milliseconds; expiry yields a timed-out outcome.</param>
/// <param name="DelayMs">Optional delay between passes in milliseconds.</param>
/// <param name="Steps">Child steps executed each pass; never empty.</param>
public sealed record WhileStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert,
    AssertionDefinition Condition,
    double TimeoutMs,
    double? DelayMs,
    IReadOnlyList<StepDefinition> Steps)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "while";
}
