namespace JTest.Language.Reading;

/// <summary>Reads and validates JTest suite documents.</summary>
public interface ISuiteDocumentReader
{
    /// <summary>Parses, binds, and validates one suite document.</summary>
    /// <param name="sourceName">Name or path used in diagnostics.</param>
    /// <param name="json">The document text.</param>
    SuiteReadResult Read(string sourceName, string json);
}
