using JTest.Core.Reporting;
using JTest.Core.Security;
using Xunit;

namespace JTest.UnitTests.Reporting;

public class VariableDumpTests
{
    [Fact]
    public void Build_MasksSecretKeys_KeepsOthers()
    {
        var dump = VariableDump.Build(
            new Dictionary<string, object?> { ["API_URL"] = "https://x", ["API_KEY"] = "s3cr3t" },
            new Dictionary<string, object?> { ["token"] = "abc", ["region"] = "eu" });

        Assert.NotNull(dump);
        Assert.Equal("https://x", dump!["env.API_URL"]);
        Assert.Equal(ValueRedactor.Mask, dump["env.API_KEY"]);
        Assert.Equal(ValueRedactor.Mask, dump["globals.token"]);
        Assert.Equal("eu", dump["globals.region"]);
    }

    [Fact]
    public void Build_WhenEmpty_IsNull()
    {
        Assert.Null(VariableDump.Build(null, null));
    }
}
