using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Expressions;
using JTest.Engine.Tracing;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;

namespace JTest.Engine.Execution;

/// <summary>Executes wait steps through the delay scheduler.</summary>
internal sealed class WaitStepRunner
{
    private readonly StepServices services;

    internal WaitStepRunner(StepServices services)
    {
        this.services = services;
    }

    internal async Task<JsonNode?> Execute(
        WaitStepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        var resolved = ExpressionResolver.ResolveValue(step.Ms, frame, services.Source);
        if (!resolved.Success)
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.AddDiagnostic(resolved.Diagnostic!);
            return null;
        }

        if (!TryToMilliseconds(resolved.Value, out var milliseconds))
        {
            node.RecordOutcome(TraceOutcome.Failed);
            node.AddDiagnostic(new LanguageDiagnostic(
                RuntimeDiagnosticCodes.ValueTypeMismatch,
                DiagnosticSeverity.Error,
                $"Wait 'ms' resolved to '{resolved.Value?.ToJsonString() ?? "null"}', which is not a non-negative number.",
                services.Source,
                string.Empty));
            return null;
        }

        await services.Delays.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken).ConfigureAwait(false);

        node.Evidence = new JsonObject { ["requestedMs"] = milliseconds };
        return new JsonObject { ["ms"] = milliseconds };
    }

    private static bool TryToMilliseconds(JsonNode? value, out double milliseconds)
    {
        milliseconds = 0;
        if (value is null)
        {
            return false;
        }

        if (value.GetValueKind() == JsonValueKind.Number)
        {
            milliseconds = value.GetValue<double>();
        }
        else if (value.GetValueKind() == JsonValueKind.String &&
                 double.TryParse(
                     value.GetValue<string>(),
                     System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture,
                     out var parsed))
        {
            milliseconds = parsed;
        }
        else
        {
            return false;
        }

        return milliseconds >= 0;
    }
}
