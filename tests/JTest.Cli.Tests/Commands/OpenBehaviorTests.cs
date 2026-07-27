using JTest.Cli.Commands;

namespace JTest.Cli.Tests.Commands;

[TestClass]
public sealed class OpenBehaviorTests
{
    private static CliEnvironment Interactive() => new("C:\\work", null, false);

    [TestMethod]
    public void DefaultsToOpeningInInteractiveSessions() =>
        Assert.IsTrue(OpenBehavior.ShouldOpen(false, false, Interactive()));

    [TestMethod]
    public void CiSuppressesOpening() =>
        Assert.IsFalse(OpenBehavior.ShouldOpen(false, false, new CliEnvironment("C:\\work", "true", false)));

    [TestMethod]
    public void RedirectedOutputSuppressesOpening() =>
        Assert.IsFalse(OpenBehavior.ShouldOpen(false, false, new CliEnvironment("C:\\work", null, true)));

    [TestMethod]
    public void ExplicitOpenWinsOverCi() =>
        Assert.IsTrue(OpenBehavior.ShouldOpen(true, false, new CliEnvironment("C:\\work", "true", true)));

    [TestMethod]
    public void ExplicitNoOpenWinsOverEverything() =>
        Assert.IsFalse(OpenBehavior.ShouldOpen(true, true, Interactive()));
}
