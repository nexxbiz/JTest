using JTest.Cli.Console;

namespace JTest.Cli.Tests.Console;

[TestClass]
public sealed class JTestInvocationMapperTests
{
    [TestMethod]
    public void MapsEveryRunValueUnderItsCanonicalOptionName()
    {
        var invocation = JTestInvocationMapper.Map(new JTestRunRequest(
            ["a.json", "b.json"],
            "env.json",
            ["k=v", "x=y"],
            "globals.json",
            ["SECRET"],
            "standalone",
            "reports",
            "out",
            "4",
            "2500",
            open: true,
            noOpen: false,
            diagnostics: "json"));

        Assert.AreEqual("run", invocation.Command);
        string[] expectedPatterns = ["a.json", "b.json"];
        CollectionAssert.AreEqual(expectedPatterns, invocation.Arguments.ToArray());
        Assert.AreEqual("env.json", invocation.LastValue("env-file"));
        string[] expectedEnv = ["k=v", "x=y"];
        CollectionAssert.AreEqual(expectedEnv, invocation.Values("env").ToArray());
        Assert.AreEqual("globals.json", invocation.LastValue("globals-file"));
        string[] expectedSecrets = ["SECRET"];
        CollectionAssert.AreEqual(expectedSecrets, invocation.Values("secret-env").ToArray());
        Assert.AreEqual("standalone", invocation.LastValue("report"));
        Assert.AreEqual("reports", invocation.LastValue("report-dir"));
        Assert.AreEqual("out", invocation.LastValue("report-out"));
        Assert.AreEqual("4", invocation.LastValue("parallel"));
        Assert.AreEqual("2500", invocation.LastValue("timeout"));
        Assert.IsTrue(invocation.HasFlag("open"));
        Assert.IsFalse(invocation.HasFlag("no-open"));
        Assert.AreEqual("json", invocation.LastValue("diagnostics"));
    }

    [TestMethod]
    public void OmitsAbsentRunValuesEntirely()
    {
        var invocation = JTestInvocationMapper.Map(new JTestRunRequest(
            ["suite.json"],
            null,
            [],
            null,
            [],
            "catalog",
            null,
            null,
            "1",
            null,
            open: false,
            noOpen: false,
            diagnostics: "text"));

        Assert.IsNull(invocation.LastValue("env-file"));
        Assert.AreEqual(0, invocation.Values("env").Count);
        Assert.IsNull(invocation.LastValue("globals-file"));
        Assert.IsNull(invocation.LastValue("report-dir"));
        Assert.IsNull(invocation.LastValue("report-out"));
        Assert.IsNull(invocation.LastValue("timeout"));
        Assert.IsFalse(invocation.HasFlag("open"));
        Assert.IsFalse(invocation.HasFlag("no-open"));
    }

    [TestMethod]
    public void MapsValidateAndDescribeRequests()
    {
        var validate = JTestInvocationMapper.Map(
            new JTestValidateRequest(["suite.json"], "json"));
        var describe = JTestInvocationMapper.Map(
            new JTestDescribeRequest("suite", "-"));

        Assert.AreEqual("validate", validate.Command);
        string[] expectedValidatePatterns = ["suite.json"];
        CollectionAssert.AreEqual(expectedValidatePatterns, validate.Arguments.ToArray());
        Assert.AreEqual("json", validate.LastValue("diagnostics"));
        Assert.AreEqual("describe", describe.Command);
        Assert.AreEqual(0, describe.Arguments.Count);
        Assert.AreEqual("suite", describe.LastValue("schema"));
        Assert.AreEqual("-", describe.LastValue("output"));
    }
}
