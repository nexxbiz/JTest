using System.Text;
using JTest.Core.Tracing;

namespace JTest.Core.Reporting.Html;

/// <summary>
/// Projects oversized or binary HTTP bodies into a safe, bounded form for the report (FR-023).
/// Large bodies are truncated with an indicator and original size; binary/non-UTF-8-looking
/// content is summarized as a type/size note rather than emitted raw. This is an explicit,
/// documented view reduction of the projection only.
/// </summary>
internal static class BodyTruncation
{
    public static ExecutionTrace Apply(ExecutionTrace trace, int maxBytes) =>
        trace with { Suites = trace.Suites.Select(s => Suite(s, maxBytes)).ToList() };

    private static SuiteResult Suite(SuiteResult s, int max) =>
        s with { Cases = s.Cases.Select(c => Case(c, max)).ToList() };

    private static CaseResult Case(CaseResult c, int max) =>
        c with { Datasets = c.Datasets.Select(d => Dataset(d, max)).ToList() };

    private static DatasetResult Dataset(DatasetResult d, int max) =>
        d with { Steps = d.Steps.Select(st => Step(st, max)).ToList() };

    private static StepNode Step(StepNode n, int max) => n with
    {
        Http = n.Http is null ? null : n.Http with
        {
            RequestBody = Body(n.Http.RequestBody, max),
            ResponseBody = Body(n.Http.ResponseBody, max)
        },
        Children = n.Children?.Select(c => Step(c, max)).ToList(),
        Iterations = n.Iterations?.Select(it => it with { Steps = it.Steps.Select(s => Step(s, max)).ToList() }).ToList()
    };

    public static string? Body(string? body, int maxBytes)
    {
        if (body is null) return null;

        if (LooksBinary(body))
            return $"[binary content, {Encoding.UTF8.GetByteCount(body)} bytes]";

        var bytes = Encoding.UTF8.GetByteCount(body);
        if (bytes <= maxBytes) return body;

        var keep = Math.Min(body.Length, maxBytes);
        return body[..keep] + $"\n…[truncated, original {bytes} bytes]";
    }

    private static bool LooksBinary(string s)
    {
        if (s.Length == 0) return false;
        var sample = Math.Min(s.Length, 2048);
        var control = 0;
        for (var i = 0; i < sample; i++)
        {
            var ch = s[i];
            if (ch == '\0') return true;
            if (char.IsControl(ch) && ch != '\t' && ch != '\n' && ch != '\r') control++;
        }
        return control > sample * 0.1;
    }
}
