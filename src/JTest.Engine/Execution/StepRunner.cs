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
/// Executes step sequences and records one truthful trace node per step,
/// assertion, and skip. A step that fails or errors skips the remainder of
/// its frame — visibly, never silently.
/// </summary>
internal sealed class StepRunner
{
    private readonly StepServices services;
    private readonly HttpStepRunner httpRunner;
    private readonly WaitStepRunner waitRunner;

    internal StepRunner(StepServices services)
    {
        this.services = services;
        httpRunner = new HttpStepRunner(services);
        waitRunner = new WaitStepRunner(services);
    }

    /// <summary>Runs the steps sequentially under the parent node; returns whether all passed.</summary>
    internal async Task ExecuteSteps(
        IReadOnlyList<StepDefinition> steps,
        ExecutionFrame frame,
        TraceNode parent,
        string basePath,
        CancellationToken cancellationToken)
    {
        var stopped = false;
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var node = new TraceNode(TraceNodeKind.Step, $"{basePath}/steps/{index}", index + 1)
            {
                StepType = step.Type,
                StepId = step.Id,
                DisplayName = step.Name,
                TemplateName = (step as UseStepDefinition)?.Template,
            };
            parent.AddChild(node);

            if (stopped)
            {
                node.RecordOutcome(TraceOutcome.Skipped);
                continue;
            }

            node.StartUtc = services.Clock.UtcNow;
            try
            {
                await ExecuteStep(step, frame, node, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                node.RecordOutcome(TraceOutcome.Cancelled);
            }
            catch (Exception exception)
            {
                node.RecordOutcome(TraceOutcome.Error);
                node.AddDiagnostic(new LanguageDiagnostic(
                    RuntimeDiagnosticCodes.EngineFailure,
                    DiagnosticSeverity.Error,
                    $"Step execution failed unexpectedly: {exception.Message}",
                    services.Source,
                    string.Empty));
            }

            node.DurationMs = (services.Clock.UtcNow - node.StartUtc).TotalMilliseconds;
            if (node.Outcome != TraceOutcome.Passed)
            {
                stopped = true;
            }
        }
    }

    private async Task ExecuteStep(
        StepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        JsonNode? result;
        switch (step)
        {
            case HttpStepDefinition http:
                result = await httpRunner.Execute(http, frame, node, cancellationToken).ConfigureAwait(false);
                break;
            case WaitStepDefinition wait:
                result = await waitRunner.Execute(wait, frame, node, cancellationToken).ConfigureAwait(false);
                break;
            case AssertStepDefinition:
                result = new JsonObject();
                break;
            case ForStepDefinition or WhileStepDefinition or UseStepDefinition:
                result = await ExecuteComposite(step, frame, node, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown step definition '{step.Type}'.");
        }

        if (node.Outcome is TraceOutcome.Error or TraceOutcome.TimedOut or TraceOutcome.Cancelled)
        {
            return;
        }

        frame.SetStepResult(step.Id, result);
        ApplySaves(step, frame, node);
        EvaluateAssertions(step, frame, node);

        if (step is AssertStepDefinition && result is JsonObject assertResult)
        {
            assertResult["passed"] = node.Outcome == TraceOutcome.Passed;
        }
    }

    private Task<JsonNode?> ExecuteComposite(
        StepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken) =>
        CompositeStepRunner.Execute(this, services, step, frame, node, cancellationToken);

    private void ApplySaves(StepDefinition step, ExecutionFrame frame, TraceNode node)
    {
        foreach (var save in step.Save)
        {
            var resolved = ExpressionResolver.ResolveValue(save.Value, frame, services.Source);
            if (!resolved.Success)
            {
                node.RecordOutcome(TraceOutcome.Failed);
                node.AddDiagnostic(resolved.Diagnostic!);
                return;
            }

            if (!ScopeWriter.TryApply(frame, save.Key, resolved.Value))
            {
                node.RecordOutcome(TraceOutcome.Failed);
                node.AddDiagnostic(new LanguageDiagnostic(
                    RuntimeDiagnosticCodes.ValueTypeMismatch,
                    DiagnosticSeverity.Error,
                    $"Save target '{save.Key}' cannot be written because an intermediate value is not an object.",
                    services.Source,
                    string.Empty));
                return;
            }
        }
    }

    private void EvaluateAssertions(StepDefinition step, ExecutionFrame frame, TraceNode node)
    {
        for (var index = 0; index < step.Assert.Count; index++)
        {
            var outcome = AssertionEvaluator.Evaluate(step.Assert[index], frame, services.Source);
            var assertionNode = new TraceNode(TraceNodeKind.Assertion, $"{node.Path}/assertions/{index}", index + 1)
            {
                DisplayName = outcome.Description,
                Evidence = new JsonObject
                {
                    ["op"] = outcome.Operator,
                    ["actual"] = Redactor.Redact(outcome.Actual, services.Secrets),
                    ["expected"] = Redactor.Redact(outcome.Expected, services.Secrets),
                    ["message"] = Redactor.RedactText(outcome.Message, services.Secrets),
                },
            };
            assertionNode.RecordOutcome(outcome.Passed ? TraceOutcome.Passed : TraceOutcome.Failed);
            node.AddChild(assertionNode);
            node.RecordOutcome(assertionNode.Outcome);
        }
    }
}
