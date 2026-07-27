using System.Reflection;

namespace JTest.Reporting.Viewer;

/// <summary>
/// The committed static viewer, embedded at build time. The viewer is
/// hand-authored HTML/CSS/JS with no build toolchain and no external
/// requests; writers deploy these exact bytes.
/// </summary>
public static class ViewerAssets
{
    private static readonly Lazy<string> Html = new(static () => Read("index.html"));
    private static readonly Lazy<string> Css = new(static () => Read("viewer.css"));
    private static readonly Lazy<string> Js = new(static () => Read("viewer.js"));

    /// <summary>The viewer page shell.</summary>
    public static string IndexHtml => Html.Value;

    /// <summary>The viewer stylesheet.</summary>
    public static string ViewerCss => Css.Value;

    /// <summary>The viewer script.</summary>
    public static string ViewerJs => Js.Value;

    private static string Read(string name)
    {
        var assembly = typeof(ViewerAssets).Assembly;
        var resourceName = $"JTest.Reporting.Viewer.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' is missing.");
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
