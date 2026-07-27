using System.Text;
using JTest.Reporting.Canonical;
using JTest.Reporting.Viewer;

namespace JTest.Reporting.Writers;

/// <summary>
/// Default standalone writer: one self-contained HTML file (viewer CSS, JS,
/// and run data inlined) plus the canonical evidence beside it. Byte output
/// is a pure function of the result document.
/// </summary>
public sealed class StandaloneReportWriter : IStandaloneReportWriter
{
    /// <inheritdoc />
    public CatalogWriteResult Write(ResultDocument document, string outputDirectory)
    {
        var root = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(root);

        var resultJsonPath = Path.Combine(root, "result.json");
        File.WriteAllBytes(resultJsonPath, document.CanonicalBytes);

        var indexPath = Path.Combine(root, "index.html");
        File.WriteAllText(indexPath, BuildSingleFile(document), new UTF8Encoding(false));

        return new CatalogWriteResult(indexPath, resultJsonPath);
    }

    private static string BuildSingleFile(ResultDocument document)
    {
        // "</" is escaped as "<\/" (a legal JSON escape) so hostile data can
        // never terminate the inline script element.
        var data = Encoding.UTF8.GetString(document.CanonicalBytes)
            .Replace("</", "<\\/", StringComparison.Ordinal);

        var builder = new StringBuilder(ViewerAssets.IndexHtml.Length + data.Length + ViewerAssets.ViewerJs.Length);
        builder.Append(ViewerAssets.IndexHtml
            .Replace(
                "<link rel=\"stylesheet\" href=\"viewer.css\">",
                $"<style>\n{ViewerAssets.ViewerCss}\n</style>",
                StringComparison.Ordinal)
            .Replace(
                "<script src=\"catalog.js\"></script>",
                $"<script>window.__JTEST_RUN__ = {data};</script>",
                StringComparison.Ordinal)
            .Replace(
                "<script src=\"viewer.js\"></script>",
                $"<script>\n{ViewerAssets.ViewerJs}\n</script>",
                StringComparison.Ordinal));
        return builder.ToString();
    }
}
