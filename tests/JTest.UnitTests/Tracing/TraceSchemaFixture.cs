using Json.Schema;

namespace JTest.UnitTests.Tracing;

/// <summary>
/// The execution-trace JSON Schema, loaded once. <see cref="JsonSchema.FromText"/> registers the
/// document's <c>$id</c> in a process-wide registry and refuses to overwrite it, so every test that
/// validates against the contract must share this instance rather than load its own.
/// </summary>
internal static class TraceSchemaFixture
{
    private static readonly Lazy<JsonSchema> Instance =
        new(() => JsonSchema.FromText(File.ReadAllText(Path)), isThreadSafe: true);

    public static JsonSchema Schema => Instance.Value;

    public static string Path
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "JTest.sln")))
                dir = dir.Parent;

            return System.IO.Path.Combine(dir!.FullName, "specs", "001-jtest2-pipeline-reporting",
                "contracts", "execution-trace.schema.json");
        }
    }
}
