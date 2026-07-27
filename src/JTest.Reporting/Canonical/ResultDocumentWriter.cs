using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using JTest.Engine.Tracing;
using JTest.Language;
using Orbyss.ProgramKit.Serialization.Json.Canonicalization;

namespace JTest.Reporting.Canonical;

/// <summary>
/// Default canonical writer: projects the trace, derives the run id from
/// the canonical trace digest, and emits RFC 8785 canonical bytes through
/// the Program Kit canonicalizer.
/// </summary>
public sealed class ResultDocumentWriter : IResultDocumentWriter
{
    /// <summary>The result document contract version.</summary>
    public const string ResultVersion = "2.0.0";

    private readonly IProgramKitJsonCanonicalizer canonicalizer;

    /// <summary>Creates the writer.</summary>
    /// <param name="canonicalizer">The Program Kit canonicalizer.</param>
    public ResultDocumentWriter(IProgramKitJsonCanonicalizer canonicalizer)
    {
        this.canonicalizer = canonicalizer;
    }

    /// <inheritdoc />
    public ResultDocument Write(TraceNode run)
    {
        var trace = TraceJson.ToJson(run);
        var traceCanonical = canonicalizer.Canonicalize(
            Encoding.UTF8.GetBytes(trace.ToJsonString()),
            ReportingLimits.Default);
        var traceDigest = traceCanonical.Digest.Value;

        var startUtc = run.StartUtc.UtcDateTime;
        var runId = string.Create(
            CultureInfo.InvariantCulture,
            $"{startUtc:yyyyMMdd'T'HHmmssfff'Z'}-{traceDigest[7..15]}");

        var document = new JsonObject
        {
            ["result"] = "jtest-run",
            ["resultVersion"] = ResultVersion,
            ["language"] = LanguageContract.LanguageVersion,
            ["toolVersion"] = ToolVersion(),
            ["runId"] = runId,
            ["startUtc"] = startUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            ["durationMs"] = Math.Round(run.DurationMs, 3),
            ["outcome"] = TraceJson.OutcomeName(run.Outcome),
            ["counts"] = RunCounts.Compute(run),
            ["trace"] = trace,
        };

        var canonical = canonicalizer.Canonicalize(
            Encoding.UTF8.GetBytes(document.ToJsonString()),
            ReportingLimits.Default);

        return new ResultDocument(runId, canonical.ToArray(), canonical.Digest.Value);
    }

    private static string ToolVersion() =>
        typeof(ResultDocumentWriter).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}
