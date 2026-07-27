using JTest.Engine.Tests.TestSupport;
using JTest.Engine.Tracing;

namespace JTest.Engine.Tests.Execution;

[TestClass]
public sealed class CompositeStepTests
{
    [TestMethod]
    public async Task EveryLoopIterationIsPreservedWithFullChildren()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "loop",
                  "steps": [
                    {
                      "type": "for",
                      "items": [ "a", "b", "c" ],
                      "as": "sku",
                      "steps": [
                        { "type": "http", "method": "GET", "url": "https://api.test/{{$.sku}}" },
                        { "type": "assert", "assert": [ { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 200 } ] }
                      ]
                    }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, "{}").EnqueueJson(200, "{}").EnqueueJson(200, "{}");

        var run = await RunHarness.Run(suite, transport);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        var loop = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(3, loop.Children.Count);
        for (var index = 0; index < 3; index++)
        {
            var iteration = loop.Children[index];
            Assert.AreEqual(TraceNodeKind.Iteration, iteration.Kind);
            Assert.AreEqual(index, iteration.IterationIndex);
            Assert.AreEqual($"suites/0/cases/0/steps/0/iterations/{index}", iteration.Path);
            Assert.AreEqual(2, iteration.Children.Count);
        }

        Assert.AreEqual(3, transport.Requests.Count);
        Assert.AreEqual("https://api.test/c", transport.Requests[2].Url);
    }

    [TestMethod]
    public async Task LoopFailureRecordsPartialIterationAndSkipsTheRest()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "loop",
                  "steps": [
                    {
                      "type": "for",
                      "items": [ 1, 2, 3 ],
                      "steps": [
                        {
                          "type": "http", "method": "GET", "url": "https://api.test/{{$.item}}",
                          "assert": [ { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 200 } ]
                        },
                        { "type": "wait", "ms": 0 }
                      ]
                    },
                    { "type": "wait", "ms": 0 }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, "{}").EnqueueJson(503, "{}");

        var run = await RunHarness.Run(suite, transport);

        Assert.AreEqual(TraceOutcome.Failed, run.Outcome);
        RunHarness.AssertNoFalseGreen(run);

        var loop = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(3, loop.Children.Count, "All three iterations must appear in the trace.");
        Assert.AreEqual(TraceOutcome.Passed, loop.Children[0].Outcome);
        Assert.AreEqual(TraceOutcome.Failed, loop.Children[1].Outcome);
        Assert.AreEqual(TraceOutcome.Failed, loop.Children[1].Children[0].Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, loop.Children[1].Children[1].Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, loop.Children[2].Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, RunHarness.Descend(run, 0, 0, 1).Outcome);
    }

    [TestMethod]
    public async Task WhilePollsUntilConditionStopsHolding()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "poll",
                  "steps": [
                    {
                      "type": "while",
                      "condition": { "op": "notEquals", "actual": "{{$.this.response.body.state}}", "expected": "ready" },
                      "timeoutMs": 60000,
                      "delayMs": 5,
                      "steps": [
                        { "type": "http", "method": "GET", "url": "https://api.test/job" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, """{ "state": "pending" }""")
            .EnqueueJson(200, """{ "state": "pending" }""")
            .EnqueueJson(200, """{ "state": "ready" }""");

        var run = await RunHarness.Run(suite, transport);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        var loop = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(3, loop.Children.Count);
        Assert.AreEqual(3, transport.Requests.Count);
        Assert.AreEqual(3, loop.Evidence!["passes"]!.GetValue<int>());
        Assert.IsFalse(loop.Evidence!["timedOut"]!.GetValue<bool>());
    }

    [TestMethod]
    public async Task WhileTimeoutBecomesTimedOutWithAllPassesPreserved()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "poll forever",
                  "steps": [
                    {
                      "type": "while",
                      "condition": { "op": "equals", "actual": "{{$.this.response.body.state}}", "expected": "pending" },
                      "timeoutMs": 100,
                      "steps": [
                        { "type": "http", "method": "GET", "url": "https://api.test/job" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport();
        for (var i = 0; i < 50; i++)
        {
            transport.EnqueueJson(200, """{ "state": "pending" }""");
        }

        var run = await RunHarness.Run(suite, transport);

        Assert.AreEqual(TraceOutcome.TimedOut, run.Outcome);
        var loop = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(TraceOutcome.TimedOut, loop.Outcome);
        Assert.IsTrue(loop.Children.Count >= 1);
        Assert.IsTrue(loop.Children.All(static c => c.Outcome == TraceOutcome.Passed),
            "Every executed pass stays in the trace as evidence.");
        Assert.IsTrue(loop.Evidence!["timedOut"]!.GetValue<bool>());
    }

    private const string AuthTemplates = """
        {
          "jtest": "2.0",
          "components": {
            "templates": [
              {
                "name": "authenticate",
                "params": {
                  "baseUrl": { "type": "string", "required": true },
                  "user": { "type": "string", "default": "ci-user" }
                },
                "steps": [
                  {
                    "type": "http",
                    "method": "POST",
                    "url": "{{$.baseUrl}}/auth/login",
                    "body": { "user": "{{$.user}}" },
                    "save": { "$.ctx.token": "{{$.this.response.body.token}}" }
                  }
                ],
                "output": { "token": "{{$.ctx.token}}" }
              }
            ]
          }
        }
        """;

    [TestMethod]
    public async Task TemplateInvocationExposesOutputsAndFullInnerTrace()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "using": [ "templates.json" ],
              "env": { "baseUrl": "https://api.test" },
              "tests": [
                {
                  "name": "login then call",
                  "steps": [
                    { "type": "use", "id": "login", "template": "authenticate", "with": { "baseUrl": "{{$.env.baseUrl}}" } },
                    {
                      "type": "http", "method": "GET", "url": "{{$.env.baseUrl}}/me",
                      "headers": { "Authorization": "Bearer {{$.login.token}}" },
                      "assert": [ { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 200 } ]
                    }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, """{ "token": "tok-123" }""")
            .EnqueueJson(200, "{}");

        var run = await RunHarness.Run(suite, transport, templatesJson: AuthTemplates);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        var useStep = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual("use", useStep.StepType);
        var invocation = useStep.Children[0];
        Assert.AreEqual(TraceNodeKind.TemplateInvocation, invocation.Kind);
        Assert.AreEqual("authenticate", invocation.TemplateName);
        Assert.AreEqual(1, invocation.Children.Count);
        Assert.AreEqual("http", invocation.Children[0].StepType);
        Assert.AreEqual("suites/0/cases/0/steps/0/invocation/steps/0", invocation.Children[0].Path);
        Assert.AreEqual("Bearer tok-123", transport.Requests[1].AuthorizationHeader);
    }

    [TestMethod]
    public async Task TemplateInsideLoopKeepsTruthfulAncestry()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "using": [ "templates.json" ],
              "env": { "baseUrl": "https://api.test" },
              "tests": [
                {
                  "name": "login per tenant",
                  "steps": [
                    {
                      "type": "for",
                      "items": [ "alpha", "beta" ],
                      "as": "tenant",
                      "steps": [
                        { "type": "use", "template": "authenticate", "with": { "baseUrl": "{{$.env.baseUrl}}", "user": "{{$.tenant}}" } }
                      ]
                    }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport()
            .EnqueueJson(200, """{ "token": "t1" }""")
            .EnqueueJson(200, """{ "token": "t2" }""");

        var run = await RunHarness.Run(suite, transport, templatesJson: AuthTemplates);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        var loop = RunHarness.Descend(run, 0, 0, 0);
        var innerHttp = RunHarness.Descend(loop, 1, 0, 0, 0);
        Assert.AreEqual(
            "suites/0/cases/0/steps/0/iterations/1/steps/0/invocation/steps/0",
            innerHttp.Path);
        RunHarness.AssertNoFalseGreen(run);
    }
}
