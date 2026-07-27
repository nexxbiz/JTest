using System.Globalization;
using System.Text.Json.Nodes;
using JTest.Engine.Tracing;

namespace JTest.Reporting.Canonical;

/// <summary>
/// Static projection of trace nodes into JSON. The projection is a pure
/// function of the trace: no clocks, culture, or environment are read, so
/// identical traces yield identical documents.
/// </summary>
public static class TraceJson
{
    /// <summary>Projects one trace node (and its subtree) into JSON.</summary>
    /// <param name="node">The node to project.</param>
    public static JsonObject ToJson(TraceNode node)
    {
        var json = new JsonObject
        {
            ["path"] = node.Path,
            ["kind"] = KindName(node.Kind),
            ["ordinal"] = node.Ordinal,
            ["outcome"] = OutcomeName(node.Outcome),
            ["startUtc"] = node.StartUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            ["durationMs"] = Math.Round(node.DurationMs, 3),
        };

        if (node.IterationIndex is not null)
        {
            json["iterationIndex"] = node.IterationIndex.Value;
        }

        if (node.StepType is not null)
        {
            json["stepType"] = node.StepType;
        }

        if (node.StepId is not null)
        {
            json["stepId"] = node.StepId;
        }

        if (node.DisplayName is not null)
        {
            json["name"] = node.DisplayName;
        }

        if (node.TemplateName is not null)
        {
            json["template"] = node.TemplateName;
        }

        if (node.DatasetName is not null)
        {
            json["dataset"] = node.DatasetName;
        }

        if (node.Diagnostics.Count > 0)
        {
            var diagnostics = new JsonArray();
            foreach (var diagnostic in node.Diagnostics)
            {
                var entry = new JsonObject
                {
                    ["code"] = diagnostic.Code,
                    ["severity"] = diagnostic.Severity == JTest.Language.Diagnostics.DiagnosticSeverity.Error
                        ? "error"
                        : "warning",
                    ["message"] = diagnostic.Message,
                    ["source"] = diagnostic.Source,
                    ["pointer"] = diagnostic.JsonPointer,
                };
                if (diagnostic.Hint is not null)
                {
                    entry["hint"] = diagnostic.Hint;
                }

                diagnostics.Add(entry);
            }

            json["diagnostics"] = diagnostics;
        }

        if (node.Evidence is not null)
        {
            json["evidence"] = node.Evidence.DeepClone();
        }

        if (node.Children.Count > 0)
        {
            var children = new JsonArray();
            foreach (var child in node.Children)
            {
                children.Add(ToJson(child));
            }

            json["children"] = children;
        }

        return json;
    }

    /// <summary>Returns the canonical camel-case name of a node kind.</summary>
    /// <param name="kind">The node kind.</param>
    public static string KindName(TraceNodeKind kind) => kind switch
    {
        TraceNodeKind.Run => "run",
        TraceNodeKind.Suite => "suite",
        TraceNodeKind.Case => "case",
        TraceNodeKind.DatasetRun => "datasetRun",
        TraceNodeKind.Step => "step",
        TraceNodeKind.TemplateInvocation => "templateInvocation",
        TraceNodeKind.Iteration => "iteration",
        TraceNodeKind.Assertion => "assertion",
        _ => "unknown",
    };

    /// <summary>Returns the canonical camel-case name of an outcome.</summary>
    /// <param name="outcome">The outcome.</param>
    public static string OutcomeName(TraceOutcome outcome) => outcome switch
    {
        TraceOutcome.Passed => "passed",
        TraceOutcome.Failed => "failed",
        TraceOutcome.Error => "error",
        TraceOutcome.Skipped => "skipped",
        TraceOutcome.Cancelled => "cancelled",
        TraceOutcome.TimedOut => "timedOut",
        _ => "unknown",
    };
}
