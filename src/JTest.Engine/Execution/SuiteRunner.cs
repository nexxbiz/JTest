using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Expressions;
using JTest.Engine.Ports;
using JTest.Engine.Redaction;
using JTest.Engine.Tracing;
using JTest.Language.Documents;
using JTest.Language.Semantics;

namespace JTest.Engine.Execution;

/// <summary>
/// Executes validated suite bundles and produces the run trace — the single
/// immutable evidence tree. Suites are independent (globals are
/// suite-scoped), so they may run concurrently; cases and dataset runs
/// within a suite are always sequential.
/// </summary>
public sealed class SuiteRunner
{
    private readonly IHttpTransport transport;
    private readonly IEngineClock clock;
    private readonly IDelayScheduler delays;
    private readonly IProcessEnvironment environment;

    /// <summary>Creates the runner with its collaborators.</summary>
    /// <param name="transport">HTTP transport.</param>
    /// <param name="clock">Engine clock for timing evidence.</param>
    /// <param name="delays">Delay scheduler.</param>
    /// <param name="environment">Process environment for <c>${NAME}</c> substitution.</param>
    public SuiteRunner(
        IHttpTransport transport,
        IEngineClock clock,
        IDelayScheduler delays,
        IProcessEnvironment environment)
    {
        this.transport = transport;
        this.clock = clock;
        this.delays = delays;
        this.environment = environment;
    }

    /// <summary>Executes every bundle and returns the sealed run node.</summary>
    /// <param name="bundles">The validated suites with their templates, in discovery order.</param>
    /// <param name="options">Run options.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    public async Task<TraceNode> ExecuteRun(
        IReadOnlyList<SuiteBundle> bundles,
        RunOptions options,
        CancellationToken cancellationToken)
    {
        var run = new TraceNode(TraceNodeKind.Run, string.Empty, 1)
        {
            StartUtc = clock.UtcNow,
        };
        run.StartUtc = clock.UtcNow;

        var suiteNodes = new TraceNode[bundles.Count];
        if (options.Parallelism > 1)
        {
            using var limiter = new SemaphoreSlim(options.Parallelism);
            var work = new List<Task>();
            for (var index = 0; index < bundles.Count; index++)
            {
                var suiteIndex = index;
                work.Add(Task.Run(
                    async () =>
                    {
                        await limiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            suiteNodes[suiteIndex] = await ExecuteSuite(
                                bundles[suiteIndex], suiteIndex, options, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            limiter.Release();
                        }
                    },
                    cancellationToken));
            }

            await Task.WhenAll(work).ConfigureAwait(false);
        }
        else
        {
            for (var index = 0; index < bundles.Count; index++)
            {
                suiteNodes[index] = await ExecuteSuite(bundles[index], index, options, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        foreach (var suiteNode in suiteNodes)
        {
            run.AddChild(suiteNode);
        }

        run.SealFromChildren();
        run.DurationMs = (clock.UtcNow - run.StartUtc).TotalMilliseconds;
        return run;
    }

    private async Task<TraceNode> ExecuteSuite(
        SuiteBundle bundle,
        int suiteIndex,
        RunOptions options,
        CancellationToken cancellationToken)
    {
        var suite = bundle.Suite;
        var path = $"suites/{suiteIndex}";
        var node = new TraceNode(TraceNodeKind.Suite, path, suiteIndex + 1)
        {
            DisplayName = suite.Info?.Name ?? bundle.SuiteSource,
            Evidence = new JsonObject { ["source"] = bundle.SuiteSource },
            StartUtc = clock.UtcNow,
        };

        var envResult = EnvironmentTokenSubstitution.Substitute(
            suite.Env, environment.GetValue, bundle.SuiteSource, "/env");
        var globalsResult = EnvironmentTokenSubstitution.Substitute(
            suite.Globals, environment.GetValue, bundle.SuiteSource, "/globals");

        foreach (var diagnostic in envResult.Diagnostics.Concat(globalsResult.Diagnostics))
        {
            node.AddDiagnostic(diagnostic);
        }

        if (!envResult.Success || !globalsResult.Success)
        {
            node.RecordOutcome(TraceOutcome.Error);
            AppendSkippedCases(node, suite, path);
            node.DurationMs = (clock.UtcNow - node.StartUtc).TotalMilliseconds;
            return node;
        }

        var env = envResult.Values;
        var globals = globalsResult.Values;
        MergeExtras(env, options.ExtraEnv);
        MergeExtras(globals, options.ExtraGlobals);

        var secrets = new SecretSet();
        var bootstrap = ExecutionFrame.CreateCase(env, globals, []);
        SecretCollector.CollectDeclared(bootstrap, suite.Secrets, secrets, bundle.SuiteSource);
        RegisterSubstitutedValues(envResult, env, secrets);
        RegisterSubstitutedValues(globalsResult, globals, secrets);
        RegisterExtraSecrets(options, secrets);

        var templates = new Dictionary<string, TemplateDefinition>(StringComparer.Ordinal);
        foreach (var (_, document) in bundle.TemplateFiles)
        {
            foreach (var template in document.Templates)
            {
                templates[template.Name] = template;
            }
        }

        var services = new StepServices(transport, clock, delays, secrets, templates, bundle.SuiteSource);
        var runner = new StepRunner(services);

        for (var caseIndex = 0; caseIndex < suite.Tests.Count; caseIndex++)
        {
            var testCase = suite.Tests[caseIndex];
            var caseNode = await ExecuteCase(
                testCase, runner, env, globals, $"{path}/cases/{caseIndex}", caseIndex, cancellationToken)
                .ConfigureAwait(false);
            node.AddChild(caseNode);
        }

        node.SealFromChildren();
        node.DurationMs = (clock.UtcNow - node.StartUtc).TotalMilliseconds;
        return node;
    }

    private async Task<TraceNode> ExecuteCase(
        TestCaseDefinition testCase,
        StepRunner runner,
        JsonObject env,
        JsonObject globals,
        string path,
        int caseIndex,
        CancellationToken cancellationToken)
    {
        var caseNode = new TraceNode(TraceNodeKind.Case, path, caseIndex + 1)
        {
            DisplayName = testCase.Name,
            StartUtc = clock.UtcNow,
        };

        if (testCase.Datasets.Count == 0)
        {
            var frame = ExecutionFrame.CreateCase(env, globals, []);
            await runner.ExecuteSteps(testCase.Steps, frame, caseNode, path, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            for (var datasetIndex = 0; datasetIndex < testCase.Datasets.Count; datasetIndex++)
            {
                var dataset = testCase.Datasets[datasetIndex];
                var datasetPath = $"{path}/datasets/{datasetIndex}";
                var datasetNode = new TraceNode(TraceNodeKind.DatasetRun, datasetPath, datasetIndex + 1)
                {
                    DatasetName = dataset.Name,
                    DisplayName = $"{testCase.Name} [{dataset.Name}]",
                    StartUtc = clock.UtcNow,
                };
                caseNode.AddChild(datasetNode);

                var caseValues = ResolveDatasetValues(dataset, env, globals, datasetNode);
                if (caseValues is not null)
                {
                    var frame = ExecutionFrame.CreateCase(env, globals, caseValues);
                    await runner.ExecuteSteps(testCase.Steps, frame, datasetNode, datasetPath, cancellationToken)
                        .ConfigureAwait(false);
                }

                datasetNode.SealFromChildren();
                datasetNode.DurationMs = (clock.UtcNow - datasetNode.StartUtc).TotalMilliseconds;
            }
        }

        caseNode.SealFromChildren();
        caseNode.DurationMs = (clock.UtcNow - caseNode.StartUtc).TotalMilliseconds;
        return caseNode;
    }

    private static JsonObject? ResolveDatasetValues(
        DatasetDefinition dataset,
        JsonObject env,
        JsonObject globals,
        TraceNode datasetNode)
    {
        var bootstrap = ExecutionFrame.CreateCase(env, globals, []);
        var values = new JsonObject();
        foreach (var entry in dataset.Case)
        {
            var resolved = ExpressionResolver.ResolveValue(entry.Value, bootstrap, datasetNode.Path);
            if (!resolved.Success)
            {
                datasetNode.RecordOutcome(TraceOutcome.Error);
                datasetNode.AddDiagnostic(resolved.Diagnostic!);
                return null;
            }

            values[entry.Key] = resolved.Value;
        }

        return values;
    }

    private void AppendSkippedCases(TraceNode suiteNode, JTestSuiteDocument suite, string path)
    {
        for (var caseIndex = 0; caseIndex < suite.Tests.Count; caseIndex++)
        {
            var caseNode = new TraceNode(TraceNodeKind.Case, $"{path}/cases/{caseIndex}", caseIndex + 1)
            {
                DisplayName = suite.Tests[caseIndex].Name,
                StartUtc = clock.UtcNow,
            };
            caseNode.RecordOutcome(TraceOutcome.Skipped);
            suiteNode.AddChild(caseNode);
        }
    }

    private static void MergeExtras(JsonObject target, IReadOnlyDictionary<string, string>? extras)
    {
        if (extras is null)
        {
            return;
        }

        foreach (var extra in extras)
        {
            target[extra.Key] = extra.Value;
        }
    }

    private static void RegisterSubstitutedValues(
        EnvironmentSubstitutionResult substitution,
        JsonObject values,
        SecretSet secrets)
    {
        foreach (var jsonPointer in substitution.SubstitutedPointers)
        {
            var node = Navigate(values, jsonPointer);
            SecretCollector.RegisterLeaves(node, secrets);
        }
    }

    private static void RegisterExtraSecrets(RunOptions options, SecretSet secrets)
    {
        if (options.ExtraSecretEnvNames is null || options.ExtraEnv is null)
        {
            return;
        }

        foreach (var name in options.ExtraSecretEnvNames)
        {
            if (options.ExtraEnv.TryGetValue(name, out var value))
            {
                secrets.Register(value);
            }
        }
    }

    private static JsonNode? Navigate(JsonObject root, string jsonPointer)
    {
        JsonNode? current = root;
        foreach (var rawSegment in jsonPointer.Split('/').Skip(2))
        {
            var segment = rawSegment
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            current = current switch
            {
                JsonObject jsonObject => jsonObject[segment],
                JsonArray jsonArray when int.TryParse(segment, out var index) &&
                                          index >= 0 && index < jsonArray.Count => jsonArray[index],
                _ => null,
            };

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
