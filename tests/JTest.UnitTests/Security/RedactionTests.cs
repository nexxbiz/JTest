using JTest.Core.Reporting;
using JTest.Core.Security;
using Xunit;

namespace JTest.UnitTests.Security;

public class RedactionTests
{
    [Fact]
    public void RegisteredSecret_IsMaskedInBodyAndQueryString()
    {
        var r = new ValueRedactor();
        r.RegisterSecret("s3cr3t-token");

        Assert.Equal($"{{\"t\":\"{ValueRedactor.Mask}\"}}", r.Redact("{\"t\":\"s3cr3t-token\"}"));
        Assert.Equal($"?token={ValueRedactor.Mask}", r.Redact("?token=s3cr3t-token"));
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("Set-Cookie")]
    [InlineData("Cookie")]
    [InlineData("password")]
    [InlineData("x-api-key")]
    public void SecretLikeKey_MasksValue(string key)
    {
        var r = new ValueRedactor();
        Assert.Equal(ValueRedactor.Mask, r.RedactValue(key, "sensitive-value"));
    }

    [Fact]
    public void NonSecretKey_PreservesValue()
    {
        var r = new ValueRedactor();
        Assert.Equal("production", r.RedactValue("environment", "production"));
        Assert.Equal("https://api.example.com", r.RedactValue("baseUrl", "https://api.example.com"));
    }

    [Fact]
    public void SecretKeyValue_IsAlsoMaskedWhereverElseItAppears()
    {
        var r = new ValueRedactor();
        r.ConsiderKeyValue("Authorization", "Bearer abc.def.ghi");

        // The same token echoed in a response body must also be masked.
        Assert.Equal($"received: {ValueRedactor.Mask}", r.Redact("received: Bearer abc.def.ghi"));
    }

    [Fact]
    public void LongestSecret_WinsToAvoidPartialLeak()
    {
        var r = new ValueRedactor();
        r.RegisterSecret("abc");
        r.RegisterSecret("abcdef");
        Assert.Equal(ValueRedactor.Mask, r.Redact("abcdef"));
    }

    [Fact]
    public void Pipeline_Html_MakesMarkupInert()
    {
        var pipeline = new ReportValuePipeline(new ValueRedactor());
        Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", pipeline.Html("<script>alert(1)</script>"));
    }

    [Fact]
    public void Pipeline_Html_RedactsBeforeEncoding()
    {
        var r = new ValueRedactor();
        r.RegisterSecret("<b>tok3n</b>"); // secret that also contains markup
        var pipeline = new ReportValuePipeline(r);

        // Redaction happens first, so nothing of the secret (or its markup) survives.
        Assert.Equal(ValueRedactor.Mask, pipeline.Html("<b>tok3n</b>"));
    }

    [Fact]
    public void Pipeline_Markdown_EscapesMarkupAndPipes()
    {
        var pipeline = new ReportValuePipeline(new ValueRedactor());
        Assert.Equal("a \\| &lt;b&gt;", pipeline.Markdown("a | <b>"));
    }
}
