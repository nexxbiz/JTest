using JTest.Reporting.Canonical;

namespace JTest.Reporting.Writers;

/// <summary>Writes one run into the persistent human reports catalog.</summary>
public interface ICatalogReportWriter
{
    /// <summary>Deploys the viewer, stores the run, and updates the catalog index.</summary>
    /// <param name="document">The canonical result document.</param>
    /// <param name="reportDirectory">The catalog directory, e.g. <c>.jtest/reports</c>.</param>
    CatalogWriteResult Write(ResultDocument document, string reportDirectory);
}
