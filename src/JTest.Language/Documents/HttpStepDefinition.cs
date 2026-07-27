namespace JTest.Language.Documents;

/// <summary>An <c>http</c> step: performs one HTTP request.</summary>
/// <param name="Id">See <see cref="StepDefinition"/>.</param>
/// <param name="Name">See <see cref="StepDefinition"/>.</param>
/// <param name="Description">See <see cref="StepDefinition"/>.</param>
/// <param name="Save">See <see cref="StepDefinition"/>.</param>
/// <param name="Assert">See <see cref="StepDefinition"/>.</param>
/// <param name="Method">HTTP method; may be an expression.</param>
/// <param name="Url">Request URL; may contain expressions.</param>
/// <param name="Headers">Request headers.</param>
/// <param name="Query">Query parameters appended to the URL.</param>
/// <param name="Body">JSON request body; mutually exclusive with <paramref name="File"/> and <paramref name="FormFiles"/>.</param>
/// <param name="File">Path of a file to send as the body.</param>
/// <param name="FormFiles">Multipart form files to send as the body.</param>
/// <param name="TimeoutMs">Optional per-request timeout in milliseconds.</param>
public sealed record HttpStepDefinition(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert,
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> Query,
    System.Text.Json.JsonElement? Body,
    string? File,
    IReadOnlyList<HttpFormFileDefinition> FormFiles,
    double? TimeoutMs)
    : StepDefinition(Id, Name, Description, Save, Assert)
{
    /// <inheritdoc />
    public override string Type => "http";
}
