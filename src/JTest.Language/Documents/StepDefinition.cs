namespace JTest.Language.Documents;

/// <summary>
/// Common shape of every step. Concrete step kinds are the closed set of
/// derived records in this folder; the <c>type</c> discriminator selects one.
/// </summary>
/// <param name="Id">Optional frame-unique id exposing the step result as <c>$.&lt;id&gt;</c>.</param>
/// <param name="Name">Optional display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Save">Save operations: target path to source value.</param>
/// <param name="Assert">Assertions evaluated after the step body.</param>
public abstract record StepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert)
{
    /// <summary>The <c>type</c> discriminator value of this step kind.</summary>
    public abstract string Type { get; }
}
