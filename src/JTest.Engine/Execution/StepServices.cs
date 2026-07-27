using JTest.Engine.Ports;
using JTest.Engine.Redaction;
using JTest.Language.Documents;

namespace JTest.Engine.Execution;

/// <summary>The shared collaborators one suite run threads through its steps.</summary>
/// <param name="Transport">HTTP transport.</param>
/// <param name="Clock">Engine clock for timing evidence.</param>
/// <param name="Delays">Delay scheduler.</param>
/// <param name="Secrets">The run's secret set for evidence redaction.</param>
/// <param name="Templates">Loaded templates by name.</param>
/// <param name="Source">Suite document name for diagnostics.</param>
internal sealed record StepServices(
    IHttpTransport Transport,
    IEngineClock Clock,
    IDelayScheduler Delays,
    SecretSet Secrets,
    IReadOnlyDictionary<string, TemplateDefinition> Templates,
    string Source);
