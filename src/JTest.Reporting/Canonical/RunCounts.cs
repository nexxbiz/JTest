using System.Text.Json.Nodes;
using JTest.Engine.Tracing;

namespace JTest.Reporting.Canonical;

/// <summary>Static aggregation of run counts from the trace.</summary>
public static class RunCounts
{
    /// <summary>Computes case-run and assertion counts for one run node.</summary>
    /// <param name="run">The sealed run node.</param>
    public static JsonObject Compute(TraceNode run)
    {
        var caseRuns = new Dictionary<TraceOutcome, int>();
        var assertionsTotal = 0;
        var assertionsFailed = 0;

        void Visit(TraceNode node)
        {
            var isCaseRun =
                node.Kind == TraceNodeKind.DatasetRun ||
                (node.Kind == TraceNodeKind.Case &&
                 node.Children.All(static child => child.Kind != TraceNodeKind.DatasetRun));

            if (isCaseRun)
            {
                caseRuns[node.Outcome] = caseRuns.GetValueOrDefault(node.Outcome) + 1;
            }

            if (node.Kind == TraceNodeKind.Assertion)
            {
                assertionsTotal++;
                if (node.Outcome != TraceOutcome.Passed)
                {
                    assertionsFailed++;
                }
            }

            foreach (var child in node.Children)
            {
                Visit(child);
            }
        }

        Visit(run);

        return new JsonObject
        {
            ["caseRuns"] = new JsonObject
            {
                ["total"] = caseRuns.Values.Sum(),
                ["passed"] = caseRuns.GetValueOrDefault(TraceOutcome.Passed),
                ["failed"] = caseRuns.GetValueOrDefault(TraceOutcome.Failed),
                ["error"] = caseRuns.GetValueOrDefault(TraceOutcome.Error),
                ["skipped"] = caseRuns.GetValueOrDefault(TraceOutcome.Skipped),
                ["cancelled"] = caseRuns.GetValueOrDefault(TraceOutcome.Cancelled),
                ["timedOut"] = caseRuns.GetValueOrDefault(TraceOutcome.TimedOut),
            },
            ["assertions"] = new JsonObject
            {
                ["total"] = assertionsTotal,
                ["failed"] = assertionsFailed,
            },
        };
    }
}
