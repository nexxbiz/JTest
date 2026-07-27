using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Assertions;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;
using JTest.Engine.Redaction;
using JTest.Engine.Tracing;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;

namespace JTest.Engine.Execution;

/// <summary>
/// Executes composite steps. Every template invocation, loop iteration, and
/// while pass is a distinct trace node with its complete children — the
/// exact execution history is preserved, never overwritten.
/// </summary>
internal static class CompositeStepRunner
{
    internal static Task<JsonNode?> Execute(
        StepRunner runner,
        StepServices services,
        StepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken) => step switch
        {
            UseStepDefinition use => ExecuteUse(runner, services, use, frame, node, cancellationToken),
            ForStepDefinition loop => ExecuteFor(runner, services, loop, frame, node, cancellationToken),
            WhileStepDefinition loop => ExecuteWhile(runner, services, loop, frame, node, cancellationToken),
            _ => throw new InvalidOperationException($"'{step.Type}' is not a composite step."),
        };

    private static async Task<JsonNode?> ExecuteUse(
        StepRunner runner,
        StepServices services,
        UseStepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        if (!services.Templates.TryGetValue(step.Template, out var template))
        {
            node.RecordOutcome(TraceOutcome.Error);
            node.AddDiagnostic(new LanguageDiagnostic(
                RuntimeDiagnosticCodes.EngineFailure,
                DiagnosticSeverity.Error,
                $"Template '{step.Template}' is not loaded; bundle validation should have rejected this document.",
                services.Source,
                string.Empty));
            return null;
        }

        var parameters = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var parameter in template.Parameters)
        {
            if (parameter.Value.Default is not null)
            {
                parameters[parameter.Key] = JsonElementNodes.ToNode(parameter.Value.Default.Value);
            }
        }

        var withEvidence = new JsonObject();
        foreach (var argument in step.With)
        {
            var resolved = ExpressionResolver.ResolveValue(argument.Value, frame, services.Source);
            if (!resolved.Success)
            {
                node.RecordOutcome(TraceOutcome.Failed);
                node.AddDiagnostic(resolved.Diagnostic!);
                return null;
            }

            parameters[argument.Key] = resolved.Value;
            withEvidence[argument.Key] = Redactor.Redact(resolved.Value, services.Secrets);
        }

        var templateFrame = ExecutionFrame.CreateTemplate(frame, parameters);
        var invocation = new TraceNode(TraceNodeKind.TemplateInvocation, $"{node.Path}/invocation", 1)
        {
            TemplateName = template.Name,
            DisplayName = template.Name,
            StartUtc = services.Clock.UtcNow,
            Evidence = new JsonObject { ["template"] = template.Name, ["with"] = withEvidence },
        };
        node.AddChild(invocation);

        await runner.ExecuteSteps(template.Steps, templateFrame, invocation, invocation.Path, cancellationToken)
            .ConfigureAwait(false);
        invocation.SealFromChildren();
        invocation.DurationMs = (services.Clock.UtcNow - invocation.StartUtc).TotalMilliseconds;
        node.RecordOutcome(invocation.Outcome);

        if (node.Outcome != TraceOutcome.Passed)
        {
            return null;
        }

        var outputs = new JsonObject();
        foreach (var output in template.Output)
        {
            var resolved = ExpressionResolver.ResolveValue(output.Value, templateFrame, services.Source);
            if (!resolved.Success)
            {
                node.RecordOutcome(TraceOutcome.Failed);
                node.AddDiagnostic(resolved.Diagnostic!);
                return null;
            }

            outputs[output.Key] = resolved.Value;
        }

        return outputs;
    }

    private static async Task<JsonNode?> ExecuteFor(
        StepRunner runner,
        StepServices services,
        ForStepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        if (!TryResolveItems(step, frame, services, node, out var items))
        {
            return null;
        }

        var completed = 0;
        for (var index = 0; index < items.Count; index++)
        {
            var iteration = new TraceNode(TraceNodeKind.Iteration, $"{node.Path}/iterations/{index}", index + 1)
            {
                IterationIndex = index,
                StartUtc = services.Clock.UtcNow,
                Evidence = new JsonObject { ["item"] = Redactor.Redact(items[index], services.Secrets) },
            };
            node.AddChild(iteration);

            if (node.Outcome != TraceOutcome.Passed)
            {
                iteration.RecordOutcome(TraceOutcome.Skipped);
                continue;
            }

            var loopFrame = ExecutionFrame.CreateLoop(frame, new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            {
                [step.As] = items[index]?.DeepClone(),
                [step.IndexAs] = JsonValue.Create(index),
            });

            await runner.ExecuteSteps(step.Steps, loopFrame, iteration, iteration.Path, cancellationToken)
                .ConfigureAwait(false);
            iteration.SealFromChildren();
            iteration.DurationMs = (services.Clock.UtcNow - iteration.StartUtc).TotalMilliseconds;
            node.RecordOutcome(iteration.Outcome);

            if (iteration.Outcome == TraceOutcome.Passed)
            {
                completed++;
                if (step.DelayMs is not null && index < items.Count - 1)
                {
                    await services.Delays.Delay(TimeSpan.FromMilliseconds(step.DelayMs.Value), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        return new JsonObject
        {
            ["items"] = items.Count,
            ["completedIterations"] = completed,
            ["allPassed"] = node.Outcome == TraceOutcome.Passed,
        };
    }

    private static async Task<JsonNode?> ExecuteWhile(
        StepRunner runner,
        StepServices services,
        WhileStepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        var start = services.Clock.UtcNow;
        var passes = 0;
        var conditionHeld = true;

        while (true)
        {
            var index = passes;
            var iteration = new TraceNode(TraceNodeKind.Iteration, $"{node.Path}/iterations/{index}", index + 1)
            {
                IterationIndex = index,
                StartUtc = services.Clock.UtcNow,
            };
            node.AddChild(iteration);

            var loopFrame = ExecutionFrame.CreateLoop(
                frame, new Dictionary<string, JsonNode?>(StringComparer.Ordinal));
            await runner.ExecuteSteps(step.Steps, loopFrame, iteration, iteration.Path, cancellationToken)
                .ConfigureAwait(false);
            iteration.SealFromChildren();
            iteration.DurationMs = (services.Clock.UtcNow - iteration.StartUtc).TotalMilliseconds;
            node.RecordOutcome(iteration.Outcome);
            passes++;

            if (node.Outcome != TraceOutcome.Passed)
            {
                break;
            }

            var condition = AssertionEvaluator.Evaluate(step.Condition, loopFrame, services.Source);
            conditionHeld = condition.Passed;
            if (!conditionHeld)
            {
                break;
            }

            if ((services.Clock.UtcNow - start).TotalMilliseconds >= step.TimeoutMs)
            {
                node.RecordOutcome(TraceOutcome.TimedOut);
                node.AddDiagnostic(new LanguageDiagnostic(
                    RuntimeDiagnosticCodes.ValueTypeMismatch,
                    DiagnosticSeverity.Error,
                    $"The while step exceeded its timeout of {step.TimeoutMs} ms after {passes} passes.",
                    services.Source,
                    string.Empty));
                break;
            }

            if (step.DelayMs is not null)
            {
                await services.Delays.Delay(TimeSpan.FromMilliseconds(step.DelayMs.Value), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        node.Evidence = new JsonObject
        {
            ["passes"] = passes,
            ["timedOut"] = node.Outcome == TraceOutcome.TimedOut,
            ["conditionHeldAtEnd"] = conditionHeld,
        };

        return new JsonObject
        {
            ["passes"] = passes,
            ["timedOut"] = node.Outcome == TraceOutcome.TimedOut,
        };
    }

    private static bool TryResolveItems(
        ForStepDefinition step,
        ExecutionFrame frame,
        StepServices services,
        TraceNode node,
        out List<JsonNode?> items)
    {
        items = [];
        var resolved = ExpressionResolver.ResolveValue(step.Items, frame, services.Source);
        if (!resolved.Success)
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.AddDiagnostic(resolved.Diagnostic!);
            return false;
        }

        if (resolved.Value is not JsonArray array)
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.AddDiagnostic(new LanguageDiagnostic(
                RuntimeDiagnosticCodes.ValueTypeMismatch,
                DiagnosticSeverity.Error,
                $"The 'items' expression resolved to {resolved.Value?.GetValueKind().ToString() ?? "null"}, not an array.",
                services.Source,
                string.Empty));
            return false;
        }

        foreach (var item in array)
        {
            items.Add(item);
        }

        return true;
    }
}
