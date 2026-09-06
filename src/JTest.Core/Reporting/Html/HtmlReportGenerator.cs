using System.Text;
using JTest.Core.Tracing;

namespace JTest.Core.Reporting.Html;

public sealed class HtmlReportOptions
{
    /// <summary>Bodies larger than this are truncated in the report (FR-023).</summary>
    public int MaxBodyBytes { get; init; } = 256 * 1024;
}

/// <summary>
/// Projects a canonical <see cref="ExecutionTrace"/> into a single self-contained HTML file
/// (FR-016/018): all CSS/JS inlined, the trace embedded as an inert JSON island, and rendered
/// client-side into a failure-first, searchable, keyboard-navigable tree. No external requests.
/// </summary>
public sealed class HtmlReportGenerator
{
    private readonly string _shell;
    private readonly string _css;
    private readonly string _js;

    public HtmlReportGenerator()
    {
        _shell = ReadAsset("report.shell.html");
        _css = ReadAsset("report.css");
        _js = ReadAsset("report.js");
    }

    public string Generate(ExecutionTrace trace, HtmlReportOptions? options = null)
    {
        options ??= new HtmlReportOptions();
        var projected = FailureFirst.Order(BodyTruncation.Apply(trace, options.MaxBodyBytes));

        // System.Text.Json's default encoder escapes '<', '>' and '&' to \uXXXX, so the embedded
        // JSON cannot contain a literal "</script>" and cannot break out of the JSON island.
        var json = TraceJson.Serialize(projected);

        return _shell
            .Replace("/*__CSS__*/", _css)
            .Replace("/*__JS__*/", _js)
            .Replace("__TRACE__", json);
    }

    public void Write(ExecutionTrace trace, string path, HtmlReportOptions? options = null)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, Generate(trace, options), new UTF8Encoding(false));
    }

    private static string ReadAsset(string suffix)
    {
        var assembly = typeof(HtmlReportGenerator).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded report asset '{suffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
