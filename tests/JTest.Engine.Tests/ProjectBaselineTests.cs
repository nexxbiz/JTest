namespace JTest.Engine.Tests;

/// <summary>
/// Baseline scaffold test proving the test host and project reference wire
/// up; replaced by real coverage in work units JT2-W030..W050.
/// </summary>
[TestClass]
public sealed class ProjectBaselineTests
{
    [TestMethod]
    public void EngineAssemblyIsReferenced()
    {
        var assemblyName = typeof(JTest.Engine.AssemblyAnchor).Assembly.GetName().Name;
        Assert.AreEqual("JTest.Engine", assemblyName);
    }
}
