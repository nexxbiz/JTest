namespace JTest.Language.Reading;

/// <summary>Reads and validates JTest template files.</summary>
public interface ITemplateFileReader
{
    /// <summary>Parses, binds, and validates one template file.</summary>
    /// <param name="sourceName">Name or path used in diagnostics.</param>
    /// <param name="json">The document text.</param>
    TemplateFileReadResult Read(string sourceName, string json);
}
