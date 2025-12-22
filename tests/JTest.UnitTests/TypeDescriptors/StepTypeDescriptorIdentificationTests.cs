using JTest.Core.TypeDescriptors;

namespace JTest.UnitTests.TypeDescriptors;

public sealed class StepTypeDescriptorIdentificationTests
{
    [Fact]
    public void When_Identify_Then_Returns_DefaultName()
    {
        // Arrange
        var sut = new StepTypeDescriptorIdentification();

        // Act
        var result = sut.Identify(typeof(MockStep));

        // Assert
        Assert.Equal("mock", result);
    }

    [Fact]
    public void When_Identify_And_TypeIdentifierAttributeSpecified_Then_Returns_IdentifierValue()
    {
        // Arrange
        var sut = new StepTypeDescriptorIdentification();

        // Act
        var result = sut.Identify(typeof(MockWithIdentifierAttributeStep));

        // Assert
        Assert.Equal("mock-step", result);
    }

    private sealed record MockStep();

    [TypeIdentifier("mock-step")]
    private sealed record MockWithIdentifierAttributeStep();
}
