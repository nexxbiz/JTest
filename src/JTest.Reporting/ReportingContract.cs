using System.Reflection;

namespace JTest.Reporting;

/// <summary>The published result-document contract artifacts.</summary>
public static class ReportingContract
{
    /// <summary>Version of the published result schema.</summary>
    public const string ResultSchemaVersion = "2.0.0";

    private static readonly Lazy<string> ResultSchema =
        new(static () => ReadResource("jtest-result-2.0.0.schema.json"));

    /// <summary>The exact bytes of the published result JSON Schema.</summary>
    public static string ResultSchemaJson => ResultSchema.Value;

    private static string ReadResource(string name)
    {
        var assembly = typeof(ReportingContract).Assembly;
        var resourceName = $"JTest.Reporting.Schemas.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' is missing.");
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
