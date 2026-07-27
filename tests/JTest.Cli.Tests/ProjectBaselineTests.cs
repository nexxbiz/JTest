namespace JTest.Cli.Tests;

/// <summary>
/// Baseline scaffold test proving the test host and project reference wire
/// up; replaced by real coverage in work unit JT2-W070.
/// </summary>
[TestClass]
public sealed class ProjectBaselineTests
{
    [TestMethod]
    public void CliAssemblyIsReferenced()
    {
        var assemblyName = typeof(JTest.Cli.Hosting.Program).Assembly.GetName().Name;
        Assert.AreEqual("jtest", assemblyName);
    }
}
