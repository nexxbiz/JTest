using JTest.Core.Reporting.Markdown;
using Xunit;

namespace JTest.UnitTests.Reporting;

public class MarkdownReportTests
{
    [Fact]
    public void Markdown_ProjectsTrace_WithNamesAndOutcomes()
    {
        var md = new MarkdownReportGenerator().Generate(TraceFixtures.Mixed());

        Assert.Contains("# JTest Report", md);
        foreach (var label in new[] { "failing-suite", "passing-suite", "authenticated flow", "check-incident", "equals" })
            Assert.Contains(label, md);
    }

    [Fact]
    public void Markdown_EscapesInjectedMarkupAndPipes()
    {
        var md = new MarkdownReportGenerator().Generate(TraceFixtures.WithText("<script>alert(1)</script>|pipe"));

        Assert.DoesNotContain("<script>", md); // HTML-encoded, not live markup
        Assert.Contains("\\|pipe", md);         // pipe escaped so it cannot break a table
    }
}
