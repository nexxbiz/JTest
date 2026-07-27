using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using JTest.Language.Tests.TestSupport;

namespace JTest.Language.Tests.Schemas;

/// <summary>
/// Proves the published JSON Schemas agree with the native validator:
/// structurally invalid fixtures fail the schema, semantically invalid ones
/// pass it (the schema cannot express them), and valid fixtures pass.
/// </summary>
[TestClass]
public sealed class SchemaAgreementTests
{
    private static readonly JsonSchema SuiteSchema = JsonSchema.FromText(LanguageContract.SuiteSchemaJson);
    private static readonly JsonSchema TemplatesSchema = JsonSchema.FromText(LanguageContract.TemplatesSchemaJson);
    private static readonly EvaluationOptions Options = new() { OutputFormat = OutputFormat.Flag };

    [TestMethod]
    public void EveryFixtureMatchesItsDeclaredSchemaVerdict()
    {
        var failures = new List<string>();
        foreach (var entry in FixtureIndex.Load())
        {
            if (entry.SchemaValid is null)
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(FixtureIndex.ReadFixture(entry.File));
            }
            catch (JsonException)
            {
                failures.Add($"{entry.File}: declared schemaValid={entry.SchemaValid} but does not parse.");
                continue;
            }

            var schema = entry.Kind == "templates" ? TemplatesSchema : SuiteSchema;
            using var parsed = document;
            var verdict = schema.Evaluate(parsed.RootElement, Options).IsValid;
            if (verdict != entry.SchemaValid)
            {
                failures.Add($"{entry.File}: schema verdict {verdict}, expected {entry.SchemaValid}.");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void EmbeddedContractArtifactsAreWellFormed()
    {
        Assert.IsNotNull(JsonNode.Parse(LanguageContract.SuiteSchemaJson));
        Assert.IsNotNull(JsonNode.Parse(LanguageContract.TemplatesSchemaJson));
        var manifest = JsonNode.Parse(LanguageContract.LanguageManifestJson);
        Assert.IsNotNull(manifest);
        Assert.AreEqual(LanguageContract.LanguageVersion, manifest!["language"]!.GetValue<string>());
        Assert.AreEqual(LanguageContract.ManifestVersion, manifest["manifestVersion"]!.GetValue<string>());
    }
}
