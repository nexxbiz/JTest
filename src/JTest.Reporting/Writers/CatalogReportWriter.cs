using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Reporting.Canonical;
using JTest.Reporting.Viewer;

namespace JTest.Reporting.Writers;

/// <summary>
/// Default catalog writer. Output bytes are a pure function of the result
/// document and the prior catalog: the writer reads no clocks, environment,
/// or culture, so identical inputs yield identical files. The viewer assets
/// are always rewritten to the exact embedded bytes.
/// </summary>
public sealed class CatalogReportWriter : ICatalogReportWriter
{
    private const string CatalogPrefix = "window.__JTEST_CATALOG__ = ";
    private const string RunPrefix = "window.__JTEST_RUN__ = ";

    /// <inheritdoc />
    public CatalogWriteResult Write(ResultDocument document, string reportDirectory)
    {
        var root = Path.GetFullPath(reportDirectory);
        Directory.CreateDirectory(root);

        WriteText(Path.Combine(root, "index.html"), ViewerAssets.IndexHtml);
        WriteText(Path.Combine(root, "viewer.css"), ViewerAssets.ViewerCss);
        WriteText(Path.Combine(root, "viewer.js"), ViewerAssets.ViewerJs);

        var runDirectory = Path.Combine(root, "runs", document.RunId);
        Directory.CreateDirectory(runDirectory);

        var resultJsonPath = Path.Combine(runDirectory, "result.json");
        File.WriteAllBytes(resultJsonPath, document.CanonicalBytes);

        var documentJson = Encoding.UTF8.GetString(document.CanonicalBytes);
        WriteText(Path.Combine(runDirectory, "result.js"), $"{RunPrefix}{documentJson};\n");

        var catalogPath = Path.Combine(root, "catalog.js");
        var catalog = LoadCatalog(catalogPath);
        UpsertEntry(catalog, document);
        WriteText(catalogPath, $"{CatalogPrefix}{catalog.ToJsonString()};\n");

        return new CatalogWriteResult(Path.Combine(root, "index.html"), resultJsonPath);
    }

    private static JsonObject LoadCatalog(string catalogPath)
    {
        if (File.Exists(catalogPath))
        {
            var text = File.ReadAllText(catalogPath, Encoding.UTF8).Trim();
            if (text.StartsWith(CatalogPrefix, StringComparison.Ordinal) &&
                text.EndsWith(';') == false)
            {
                text = text.TrimEnd();
            }

            if (text.StartsWith(CatalogPrefix, StringComparison.Ordinal))
            {
                var json = text[CatalogPrefix.Length..].TrimEnd(';', '\n', '\r');
                try
                {
                    if (JsonNode.Parse(json) is JsonObject existing)
                    {
                        return existing;
                    }
                }
                catch (JsonException)
                {
                    // A corrupt catalog is rebuilt from scratch; run evidence is untouched.
                }
            }
        }

        return new JsonObject
        {
            ["catalog"] = "jtest-runs",
            ["catalogVersion"] = "1.0.0",
            ["runs"] = new JsonArray(),
        };
    }

    private static void UpsertEntry(JsonObject catalog, ResultDocument document)
    {
        var parsed = JsonNode.Parse(Encoding.UTF8.GetString(document.CanonicalBytes))!.AsObject();

        var suiteNames = new JsonArray();
        if (parsed["trace"]?["children"] is JsonArray suites)
        {
            foreach (var suite in suites)
            {
                suiteNames.Add(suite?["name"]?.GetValue<string>() ?? "suite");
            }
        }

        var entry = new JsonObject
        {
            ["runId"] = document.RunId,
            ["outcome"] = parsed["outcome"]!.GetValue<string>(),
            ["startUtc"] = parsed["startUtc"]!.GetValue<string>(),
            ["durationMs"] = parsed["durationMs"]!.GetValue<double>(),
            ["counts"] = parsed["counts"]!["caseRuns"]!.DeepClone(),
            ["suites"] = suiteNames,
        };

        var runs = catalog["runs"]!.AsArray();
        var remaining = runs.Where(run => run?["runId"]?.GetValue<string>() != document.RunId).ToList();
        remaining.Add(entry);
        remaining.Sort(static (a, b) =>
        {
            var byStart = string.CompareOrdinal(
                b?["startUtc"]?.GetValue<string>(),
                a?["startUtc"]?.GetValue<string>());
            return byStart != 0
                ? byStart
                : string.CompareOrdinal(b?["runId"]?.GetValue<string>(), a?["runId"]?.GetValue<string>());
        });

        var rebuilt = new JsonArray();
        foreach (var run in remaining)
        {
            rebuilt.Add(run?.DeepClone());
        }

        catalog["runs"] = rebuilt;
    }

    private static void WriteText(string path, string content) =>
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}
