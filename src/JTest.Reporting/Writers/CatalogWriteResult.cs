namespace JTest.Reporting.Writers;

/// <summary>Locations produced by one catalog write.</summary>
/// <param name="IndexHtmlPath">Absolute path of the viewer page.</param>
/// <param name="ResultJsonPath">Absolute path of the run's canonical evidence.</param>
public sealed record CatalogWriteResult(string IndexHtmlPath, string ResultJsonPath);
