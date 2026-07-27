using JTest.Engine.Diagnostics;
using JTest.Engine.Execution;
using JTest.Engine.Tests.TestSupport;
using JTest.Engine.Tracing;

namespace JTest.Engine.Tests.Execution;

[TestClass]
public sealed class SuiteRunnerTests
{
    private const string PassingSuite = """
        {
          "jtest": "2.0",
          "env": { "baseUrl": "https://api.test" },
          "tests": [
            {
              "name": "create",
              "steps": [
                {
                  "type": "http",
                  "id": "create",
                  "method": "POST",
                  "url": "{{$.env.baseUrl}}/orders",
                  "body": { "sku": "widget" },
                  "save": { "$.ctx.orderId": "{{$.this.response.body.id}}" },
                  "assert": [
                    { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 201 }
                  ]
                },
                {
                  "type": "assert",
                  "assert": [ { "op": "equals", "actual": "{{$.ctx.orderId}}", "expected": "o-1" } ]
                }
              ]
            }
          ]
        }
        """;

    [TestMethod]
    public async Task PassingSuiteProducesCompletePassingTrace()
    {
        var transport = new FakeHttpTransport().EnqueueJson(201, """{ "id": "o-1" }""");

        var run = await RunHarness.Run(PassingSuite, transport);

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
        RunHarness.AssertNoFalseGreen(run);

        var step = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual("suites/0/cases/0/steps/0", step.Path);
        Assert.AreEqual("http", step.StepType);
        Assert.AreEqual(1, step.Ordinal);
        Assert.AreEqual(TraceNodeKind.Assertion, step.Children[0].Kind);
        Assert.AreEqual("https://api.test/orders", transport.Requests[0].Url);
        Assert.AreEqual(201, step.Evidence!["response"]!["status"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task FailingAssertionFailsUpwardAndSkipsRemainingSteps()
    {
        var transport = new FakeHttpTransport().EnqueueJson(500, """{ "id": "o-1" }""");

        var run = await RunHarness.Run(PassingSuite, transport);

        Assert.AreEqual(TraceOutcome.Failed, run.Outcome);
        RunHarness.AssertNoFalseGreen(run);

        var firstStep = RunHarness.Descend(run, 0, 0, 0);
        var secondStep = RunHarness.Descend(run, 0, 0, 1);
        Assert.AreEqual(TraceOutcome.Failed, firstStep.Outcome);
        Assert.AreEqual(TraceOutcome.Failed, firstStep.Children[0].Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, secondStep.Outcome);
        Assert.AreEqual(1, transport.Requests.Count);
    }

    [TestMethod]
    public async Task TransportExceptionBecomesErrorNodeNeverGreen()
    {
        var transport = new FakeHttpTransport().EnqueueThrow(new InvalidOperationException("socket exploded"));

        var run = await RunHarness.Run(PassingSuite, transport);

        Assert.AreEqual(TraceOutcome.Error, run.Outcome);
        var step = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(TraceOutcome.Error, step.Outcome);
        Assert.AreEqual(RuntimeDiagnosticCodes.EngineFailure, step.Diagnostics[0].Code);
        RunHarness.AssertNoFalseGreen(run);
    }

    [TestMethod]
    public async Task HttpTimeoutBecomesTimedOutOutcome()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "slow",
                  "steps": [
                    { "type": "http", "method": "GET", "url": "https://api.test/slow", "timeoutMs": 50 },
                    { "type": "wait", "ms": 1 }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport().EnqueueHang();

        var run = await RunHarness.Run(suite, transport);

        Assert.AreEqual(TraceOutcome.TimedOut, run.Outcome);
        var step = RunHarness.Descend(run, 0, 0, 0);
        Assert.AreEqual(TraceOutcome.TimedOut, step.Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, RunHarness.Descend(run, 0, 0, 1).Outcome);
    }

    [TestMethod]
    public async Task CancellationBecomesCancelledOutcome()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "cancelled",
                  "steps": [
                    { "type": "http", "method": "GET", "url": "https://api.test/hang" },
                    { "type": "wait", "ms": 1 }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport().EnqueueHang();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var run = await RunHarness.Run(suite, transport, cancellationToken: cancellation.Token);

        Assert.AreEqual(TraceOutcome.Cancelled, run.Outcome);
        Assert.AreEqual(TraceOutcome.Cancelled, RunHarness.Descend(run, 0, 0, 0).Outcome);
        Assert.AreEqual(TraceOutcome.Skipped, RunHarness.Descend(run, 0, 0, 1).Outcome);
    }

    [TestMethod]
    public async Task DatasetRunsAppearAsSeparateNodes()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "tests": [
                {
                  "name": "matrix",
                  "steps": [
                    { "type": "http", "method": "GET", "url": "https://api.test/{{$.case.sku}}" }
                  ],
                  "datasets": [
                    { "name": "widget", "case": { "sku": "widget" } },
                    { "name": "gadget", "case": { "sku": "gadget" } }
                  ]
                }
              ]
            }
            """;
        var transport = new FakeHttpTransport().EnqueueJson(200, "{}").EnqueueJson(200, "{}");

        var run = await RunHarness.Run(suite, transport);

        var caseNode = RunHarness.Descend(run, 0, 0);
        Assert.AreEqual(2, caseNode.Children.Count);
        Assert.AreEqual(TraceNodeKind.DatasetRun, caseNode.Children[0].Kind);
        Assert.AreEqual("widget", caseNode.Children[0].DatasetName);
        Assert.AreEqual("gadget", caseNode.Children[1].DatasetName);
        Assert.AreEqual("https://api.test/widget", transport.Requests[0].Url);
        Assert.AreEqual("https://api.test/gadget", transport.Requests[1].Url);
    }

    [TestMethod]
    public async Task UndefinedProcessVariableFailsTheSuiteClosed()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "env": { "token": "${MISSING_VAR}" },
              "tests": [
                { "name": "never runs", "steps": [ { "type": "wait", "ms": 0 } ] }
              ]
            }
            """;

        var run = await RunHarness.Run(suite, new FakeHttpTransport());

        Assert.AreEqual(TraceOutcome.Error, run.Outcome);
        var suiteNode = run.Children[0];
        Assert.AreEqual(RuntimeDiagnosticCodes.UndefinedEnvironmentVariable, suiteNode.Diagnostics[0].Code);
        Assert.AreEqual(TraceOutcome.Skipped, suiteNode.Children[0].Outcome);
    }

    [TestMethod]
    public async Task GlobalsPersistAcrossCasesInFileOrder()
    {
        const string suite = """
            {
              "jtest": "2.0",
              "globals": { "counter": "initial" },
              "tests": [
                {
                  "name": "writer",
                  "steps": [
                    { "type": "wait", "ms": 0, "save": { "$.globals.counter": "written" } }
                  ]
                },
                {
                  "name": "reader",
                  "steps": [
                    { "type": "assert", "assert": [ { "op": "equals", "actual": "{{$.globals.counter}}", "expected": "written" } ] }
                  ]
                }
              ]
            }
            """;

        var run = await RunHarness.Run(suite, new FakeHttpTransport());

        Assert.AreEqual(TraceOutcome.Passed, run.Outcome);
    }
}
