namespace JTest.Reporting.Tests;

/// <summary>
/// Baseline scaffold test proving the test host and project reference wire
/// up; replaced by real coverage in work unit JT2-W060.
/// </summary>
[TestClass]
public sealed class ProjectBaselineTests
{
    [TestMethod]
    public void ReportingAssemblyIsReferenced()
    {
        var assemblyName = typeof(JTest.Reporting.AssemblyAnchor).Assembly.GetName().Name;
        Assert.AreEqual("JTest.Reporting", assemblyName);
    }
}
