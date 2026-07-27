using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Engine.Tracing;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;

namespace JTest.Engine.Execution;

/// <summary>
/// Composite step execution (use, for, while). This work unit (JT2-W040)
/// ships the seam only; JT2-W050 supplies the implementations. Until then
/// every composite step records an honest engine error — never a pass.
/// </summary>
internal static class CompositeStepRunner
{
    internal static Task<JsonNode?> Execute(
        StepRunner runner,
        StepServices services,
        StepDefinition step,
        ExecutionFrame frame,
        TraceNode node,
        CancellationToken cancellationToken)
    {
        _ = runner;
        _ = frame;
        _ = cancellationToken;
        node.RecordOutcome(TraceOutcome.Error);
        node.AddDiagnostic(new LanguageDiagnostic(
            RuntimeDiagnosticCodes.EngineFailure,
            DiagnosticSeverity.Error,
            $"Step type '{step.Type}' is not executable yet; composite steps land in work unit JT2-W050.",
            services.Source,
            string.Empty));
        return Task.FromResult<JsonNode?>(null);
    }
}
