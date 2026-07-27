namespace JTest.Language.Tests;

/// <summary>
/// Baseline scaffold test proving the test host and project reference wire
/// up; replaced by real coverage in work unit JT2-W020.
/// </summary>
[TestClass]
public sealed class ProjectBaselineTests
{
    [TestMethod]
    public void LanguageAssemblyIsReferenced()
    {
        var assemblyName = typeof(JTest.Language.AssemblyAnchor).Assembly.GetName().Name;
        Assert.AreEqual("JTest.Language", assemblyName);
    }
}
