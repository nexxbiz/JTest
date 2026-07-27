using System.Reflection;

namespace JTest.Language;

/// <summary>
/// The published, versioned JTest language contract: the exact language
/// discriminator and the embedded schema and manifest artifacts that agents
/// and editors consume.
/// </summary>
public static class LanguageContract
{
    /// <summary>The language discriminator every document must declare.</summary>
    public const string LanguageVersion = "2.0";

    /// <summary>Version of the published suite schema.</summary>
    public const string SuiteSchemaVersion = "2.0.0";

    /// <summary>Version of the published template-file schema.</summary>
    public const string TemplatesSchemaVersion = "2.0.0";

    /// <summary>Version of the published language manifest.</summary>
    public const string ManifestVersion = "2.0.0";

    private static readonly Lazy<string> SuiteSchema =
        new(static () => ReadResource("jtest-suite-2.0.0.schema.json"));

    private static readonly Lazy<string> TemplatesSchema =
        new(static () => ReadResource("jtest-templates-2.0.0.schema.json"));

    private static readonly Lazy<string> Manifest =
        new(static () => ReadResource("jtest-language-manifest-2.0.0.json"));

    /// <summary>The exact bytes of the published suite JSON Schema.</summary>
    public static string SuiteSchemaJson => SuiteSchema.Value;

    /// <summary>The exact bytes of the published template-file JSON Schema.</summary>
    public static string TemplatesSchemaJson => TemplatesSchema.Value;

    /// <summary>The exact bytes of the published language manifest.</summary>
    public static string LanguageManifestJson => Manifest.Value;

    private static string ReadResource(string name)
    {
        var assembly = typeof(LanguageContract).Assembly;
        var resourceName = $"JTest.Language.Schemas.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' is missing.");
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
