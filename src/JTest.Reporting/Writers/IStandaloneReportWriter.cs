using JTest.Reporting.Canonical;

namespace JTest.Reporting.Writers;

/// <summary>Writes one run as a self-contained pipeline artifact.</summary>
public interface IStandaloneReportWriter
{
    /// <summary>Writes a single-file report plus the canonical evidence beside it.</summary>
    /// <param name="document">The canonical result document.</param>
    /// <param name="outputDirectory">The target directory.</param>
    CatalogWriteResult Write(ResultDocument document, string outputDirectory);
}
