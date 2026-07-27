namespace JTest.Language.Documents;

/// <summary>One multipart form file of an <c>http</c> step.</summary>
/// <param name="Name">Form field name.</param>
/// <param name="Path">File path; may contain expressions.</param>
/// <param name="ContentType">Optional explicit content type.</param>
public sealed record HttpFormFileDefinition(
    string Name,
    string Path,
    string? ContentType);
