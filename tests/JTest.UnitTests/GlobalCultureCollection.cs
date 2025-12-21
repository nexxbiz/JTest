namespace JTest.UnitTests;

using Xunit;

[CollectionDefinition(DefinitionName, DisableParallelization = true)]
public class GlobalCultureCollection : ICollectionFixture<CultureFixture>
{
    public const string DefinitionName = "Global test culture";
}


