using System.Text.RegularExpressions;
using JTest.Core.Reporting.Html;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Reporting;

public class HtmlReportTests
{
    private static string Generate(ExecutionTrace trace, HtmlReportOptions? o = null) =>
        new HtmlReportGenerator().Generate(trace, o);

    // T030 — every node in the trace is represented in the report.
    [Fact]
    public void Report_RepresentsEveryTraceNode()
    {
        var html = Generate(TraceFixtures.Mixed());

        // Human-facing labels for suites, cases, steps, and the assertion operation are all present.
        foreach (var label in new[] { "passing-suite", "failing-suite", "reads value", "authenticated flow",
                     "get-A", "login", "check-incident", "retry", "poll", "equals" })
        {
            Assert.Contains(label, html);
        }

        // The embedded trace round-trips to the same structure (projection adds/hides nothing).
        var embedded = TraceFixtures.ExtractEmbeddedTrace(html);
        var back = TraceJson.Deserialize(embedded);
        Assert.NotNull(back);
        Assert.Equal(2, back!.Suites.Count);
    }

    // T031 — self-contained: no external resource references.
    [Fact]
    public void Report_IsSelfContained_NoExternalResources()
    {
        var html = Generate(TraceFixtures.Mixed());

        Assert.DoesNotContain("<link", html);
        Assert.DoesNotContain("<script src", html);
        Assert.DoesNotContain("@import", html);
        Assert.DoesNotContain("url(http", html);
        // Inlined assets are present.
        Assert.Contains("<style>", html);
        Assert.Contains("application/json", html);
    }

    // T032 — WCAG 2.1 AA affordances: landmarks, skip link, visible focus, both color schemes, aria.
    [Fact]
    public void Report_HasAccessibilityAffordances()
    {
        var html = Generate(TraceFixtures.Mixed());

        Assert.Contains("<main", html);
        Assert.Contains("skip-link", html);
        Assert.Contains(":focus-visible", html);
        Assert.Contains("prefers-color-scheme: dark", html);
        Assert.Contains("aria-label", html);
        Assert.Contains("lang=\"en\"", html);
    }

    // T033 — failure-first: the failing suite is ordered before the passing suite (SC-011).
    [Fact]
    public void Report_OrdersFailuresFirst()
    {
        var html = Generate(TraceFixtures.Mixed());
        var embedded = TraceFixtures.ExtractEmbeddedTrace(html);

        var failIndex = embedded.IndexOf("failing-suite", StringComparison.Ordinal);
        var passIndex = embedded.IndexOf("passing-suite", StringComparison.Ordinal);

        Assert.True(failIndex >= 0 && passIndex >= 0);
        Assert.True(failIndex < passIndex, "Failing suite should be serialized before the passing suite.");
    }

    // T034 — oversized and binary bodies are safely bounded, not emitted raw.
    [Fact]
    public void Report_TruncatesOversizedBody()
    {
        var big = new string('a', 300 * 1024);
        var html = Generate(TraceFixtures.WithResponseBody(big));

        Assert.Contains("[truncated, original", html);
        Assert.DoesNotContain(new string('a', 300 * 1024), html); // full body not embedded
    }

    [Fact]
    public void Report_SummarizesBinaryBody()
    {
        var binary = "PNG\0\0\0" + new string('', 200) + "IHDR";
        var html = Generate(TraceFixtures.WithResponseBody(binary));

        Assert.Contains("[binary content,", html);
    }

    // Guards the JSON island cannot break out of the <script> tag (default STJ encoder escapes '<').
    [Fact]
    public void Report_EscapesScriptBreakoutInValues()
    {
        var html = Generate(TraceFixtures.WithResponseBody("</script><script>alert(1)</script>"));
        var embedded = TraceFixtures.ExtractEmbeddedTrace(html);

        Assert.DoesNotContain("</script>", embedded);
        Assert.Contains("\\u003C", embedded); // '<' escaped
    }

    // T042 — XSS corpus: injected markup in names/assertions/messages is inert (never live HTML).
    [Fact]
    public void Report_MakesInjectedMarkupInert_InAllTextFields()
    {
        var html = Generate(TraceFixtures.WithText("<script>alert('x')</script><img src=x onerror=alert(1)>"));

        // Angle brackets from data are escaped, so no live tag can form (the payload survives only as
        // inert text inside the escaped JSON island, rendered via textContent).
        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("\\u003C", TraceFixtures.ExtractEmbeddedTrace(html));
    }

    // Assertion clarity + response-body viewer: the asserted subject and description are surfaced, and
    // the report includes the JSON body viewer with a copy affordance (FR-050/FR-051).
    [Fact]
    public void Report_SurfacesAssertionSubject_And_BodyViewerAffordances()
    {
        var html = Generate(TraceFixtures.Mixed());

        // Subject + description of the failing assertion travel into the embedded trace.
        var embedded = TraceFixtures.ExtractEmbeddedTrace(html);
        Assert.Contains("{{$.this.statusCode}}", embedded); // the asserted subject expression
        Assert.Contains("status is 200", embedded);          // the human description

        // The renderer provides a collapsible, JSON-aware body viewer with a copy control.
        Assert.Contains("bodybox", html);
        Assert.Contains("renderBodyBox", html);
        Assert.Contains("Copy", html);
    }
}
