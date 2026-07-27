namespace JTest.Language.Documents;

/// <summary>A <c>for</c> step: executes child steps once per item.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">See <see cref="StepDefinition"/>.</param>
/// <param name="Items">An array literal or an expression resolving to an array.</param>
/// <param name="As">Binding name of the current item (default <c>item</c>).</param>
/// <param name="IndexAs">Binding name of the current index (default <c>index</c>).</param>
/// <param name="Steps">Child steps executed each iteration; never empty.</param>
/// <param name="DelayMs">Optional delay between iterations in milliseconds.</param>
public sealed record ForStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert,
    System.Text.Json.JsonElement Items,
    string As,
    string IndexAs,
    IReadOnlyList<StepDefinition> Steps,
    double? DelayMs)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "for";
}
