using System.Text;
using JTest.Core.Security;
using JTest.Core.Tracing;

namespace JTest.Core.Reporting.Markdown;

/// <summary>
/// Projects a canonical <see cref="ExecutionTrace"/> into a Markdown report (FR-016). Like the HTML
/// report it is a read-only projection of the trace; every dynamic value passes through the
/// <see cref="ReportValuePipeline"/> so nothing is emitted unescaped (the trace is already redacted).
/// </summary>
public sealed class MarkdownReportGenerator
{
    // The trace is pre-redacted; the pipeline here only escapes for Markdown/HTML safety.
    private readonly ReportValuePipeline _values = new(new ValueRedactor());

    public string Generate(ExecutionTrace trace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# JTest Report").AppendLine();
        sb.AppendLine($"tool {_values.Markdown(trace.ToolVersion)} · schema {_values.Markdown(trace.TraceSchemaVersion)} · exit {trace.ExitCode} · outcome **{trace.Outcome}**").AppendLine();

        var c = trace.Counts;
        sb.AppendLine("| total | passed | failed | errored | cancelled | timedOut | skipped |");
        sb.AppendLine("|------:|------:|------:|------:|------:|------:|------:|");
        sb.AppendLine($"| {c.Total} | {c.Passed} | {c.Failed} | {c.Errored} | {c.Cancelled} | {c.TimedOut} | {c.Skipped} |").AppendLine();

        foreach (var suite in trace.Suites)
        {
            sb.AppendLine($"## {_values.Markdown(suite.Name)} — {suite.Outcome}");
            foreach (var diag in suite.Diagnostics ?? Enumerable.Empty<Diagnostic>())
                sb.AppendLine($"> {_values.Markdown(diag.Message)}");

            foreach (var @case in suite.Cases)
            {
                sb.AppendLine($"- **{_values.Markdown(@case.Name)}** — {@case.Outcome}");
                foreach (var dataset in @case.Datasets)
                    foreach (var step in dataset.Steps)
                        WriteStep(sb, step, 1);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void WriteStep(StringBuilder sb, StepNode step, int depth)
    {
        var indent = new string(' ', depth * 2);
        var label = _values.Markdown(step.Name ?? step.StepType);
        sb.AppendLine($"{indent}- **{label}** `#{step.Ordinal}` · {_values.Markdown(step.StepType)} · **{step.Outcome}**");

        if (!string.IsNullOrWhiteSpace(step.Description))
            sb.AppendLine($"{indent}  - _{_values.Markdown(step.Description)}_");

        if (step.Http is { } http)
            sb.AppendLine($"{indent}  - `{_values.Markdown(http.Method)} {_values.Markdown(http.Url)}` → {http.StatusCode}");

        foreach (var assertion in step.Assertions ?? Enumerable.Empty<AssertionResult>())
            WriteAssertion(sb, assertion, indent + "  ");

        foreach (var child in step.Children ?? Enumerable.Empty<StepNode>())
            WriteStep(sb, child, depth + 1);

        foreach (var iteration in step.Iterations ?? Enumerable.Empty<Iteration>())
        {
            sb.AppendLine($"{indent}  - iteration {iteration.Index} · **{iteration.Outcome}**");
            foreach (var inner in iteration.Steps)
                WriteStep(sb, inner, depth + 2);
        }
    }

    // A single readable line per assertion: what was checked (subject/description), the operation,
    // expected/actual where present, its outcome, and any failure message.
    private void WriteAssertion(StringBuilder sb, AssertionResult assertion, string indent)
    {
        var parts = new List<string>();
        if (assertion.Subject is not null) parts.Add($"subject `{_values.Markdown(assertion.Subject.ToString())}`");
        if (assertion.Expected is not null) parts.Add($"expected `{_values.Markdown(assertion.Expected.ToString())}`");
        if (assertion.Actual is not null) parts.Add($"actual `{_values.Markdown(assertion.Actual.ToString())}`");

        var detail = parts.Count > 0 ? " — " + string.Join(", ", parts) : string.Empty;
        var description = string.IsNullOrWhiteSpace(assertion.Description)
            ? string.Empty
            : $" ({_values.Markdown(assertion.Description)})";

        sb.AppendLine($"{indent}- assert `{_values.Markdown(assertion.Operation)}`{description} — **{assertion.Outcome}**{detail}");

        if (!string.IsNullOrWhiteSpace(assertion.Message))
            sb.AppendLine($"{indent}  - {_values.Markdown(assertion.Message)}");
    }
}
