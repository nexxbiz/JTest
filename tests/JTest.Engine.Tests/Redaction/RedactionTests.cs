using System.Text;
using JTest.Engine.Redaction;
using JTest.Engine.Tests.TestSupport;
using JTest.Engine.Tracing;

namespace JTest.Engine.Tests.Redaction;

[TestClass]
public sealed class RedactionTests
{
    private const string SecretValue = "super-secret-token-9000";

    private const string Suite = """
        {
          "jtest": "2.0",
          "env": { "apiToken": "${API_TOKEN}", "baseUrl": "https://api.test" },
          "secrets": [ "$.env.apiToken" ],
          "tests": [
            {
              "name": "leaky endpoint",
              "steps": [
                {
                  "type": "http",
                  "method": "GET",
                  "url": "{{$.env.baseUrl}}/echo",
                  "headers": { "Authorization": "Bearer {{$.env.apiToken}}" },
                  "assert": [
                    { "op": "equals", "actual": "{{$.this.response.body.echoedToken}}", "expected": "{{$.env.apiToken}}" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [TestMethod]
    public async Task SecretsNeverEnterTheTrace()
    {
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, $$"""{ "echoedToken": "{{SecretValue}}" }""");
        var environment = new FakeProcessEnvironment().With("API_TOKEN", SecretValue);

        var run = await RunHarness.Run(Suite, transport, environment);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        Assert.IsTrue(transport.Requests[0].AuthorizationHeader!.Contains(SecretValue, StringComparison.Ordinal),
            "The real request must carry the real secret.");

        AssertSubtreeClean(run);
    }

    [TestMethod]
    public async Task ExpectedAssertionOperandIsRedactedToo()
    {
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, $$"""{ "echoedToken": "different-value" }""");
        var environment = new FakeProcessEnvironment().With("API_TOKEN", SecretValue);

        var run = await RunHarness.Run(Suite, transport, environment);

        Assert.AreEqual(TraceOutcome.Failed, run.Outcome);
        AssertSubtreeClean(run);
    }

    [TestMethod]
    public void CredentialHeadersAreAlwaysMasked()
    {
        var secrets = new SecretSet();
        var masked = Redactor.RedactHeader("Authorization", "Bearer anything-at-all", secrets);
        Assert.IsFalse(masked.Contains("anything-at-all", StringComparison.Ordinal));
        Assert.IsTrue(masked.StartsWith("«redacted:", StringComparison.Ordinal));
    }

    private static void AssertSubtreeClean(TraceNode node)
    {
        if (node.Evidence is not null)
        {
            var serialized = node.Evidence.ToJsonString();
            Assert.IsFalse(
                serialized.Contains(SecretValue, StringComparison.Ordinal),
                $"Secret leaked into evidence of '{node.Path}': {serialized}");
        }

        foreach (var diagnostic in node.Diagnostics)
        {
            Assert.IsFalse(diagnostic.Message.Contains(SecretValue, StringComparison.Ordinal));
        }

        foreach (var child in node.Children)
        {
            AssertSubtreeClean(child);
        }
    }
}
