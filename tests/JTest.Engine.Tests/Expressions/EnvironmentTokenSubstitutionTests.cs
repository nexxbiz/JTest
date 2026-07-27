using System.Text.Json;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;

namespace JTest.Engine.Tests.Expressions;

[TestClass]
public sealed class EnvironmentTokenSubstitutionTests
{
    private static readonly string[] ExpectedSensitivePointers = ["/env/token", "/env/nested/key"];

    private static Dictionary<string, JsonElement> Map(string json)
    {
        using var document = JsonDocument.Parse(json);
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            map[property.Name] = property.Value.Clone();
        }

        return map;
    }

    [TestMethod]
    public void DefinedVariablesSubstituteAndAreReportedAsSensitive()
    {
        var values = Map("""{ "token": "${API_TOKEN}", "url": "https://x.test", "nested": { "key": "k-${API_TOKEN}" } }""");

        var result = EnvironmentTokenSubstitution.Substitute(
            values,
            name => name == "API_TOKEN" ? "s3cret" : null,
            "suite.json",
            "/env");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("s3cret", result.Values["token"]!.GetValue<string>());
        Assert.AreEqual("k-s3cret", result.Values["nested"]!["key"]!.GetValue<string>());
        CollectionAssert.AreEquivalent(
            ExpectedSensitivePointers,
            result.SubstitutedPointers.ToArray());
    }

    [TestMethod]
    public void UndefinedVariablesFailClosed()
    {
        var values = Map("""{ "token": "${NOPE}" }""");

        var result = EnvironmentTokenSubstitution.Substitute(values, _ => null, "suite.json", "/env");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(RuntimeDiagnosticCodes.UndefinedEnvironmentVariable, result.Diagnostics[0].Code);
        Assert.AreEqual("/env/token", result.Diagnostics[0].JsonPointer);
    }

    [TestMethod]
    public void ValuesWithoutTokensPassThroughUnchanged()
    {
        var values = Map("""{ "url": "https://x.test", "count": 3 }""");

        var result = EnvironmentTokenSubstitution.Substitute(values, _ => null, "suite.json", "/env");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.SubstitutedPointers.Count);
        Assert.AreEqual(3, result.Values["count"]!.GetValue<int>());
    }
}
