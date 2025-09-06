using JTest.Core;

namespace JTest.UnitTests;

public class TestRunnerTests
{
    [Fact]
    public void Version_ShouldReturnValidVersionString()
    {
        // Arrange
        var testRunner = new TestRunner();

        // Act
        var version = testRunner.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public void ValidateTestDefinition_WithValidJson_ShouldReturnTrue()
    {
        // Arrange
        var testRunner = new TestRunner();
        var validJson = "{\"test\": \"value\"}";

        // Act
        var result = testRunner.ValidateTestDefinition(validJson);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateTestDefinition_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var testRunner = new TestRunner();
        var invalidJson = "not valid json";

        // Act
        var result = testRunner.ValidateTestDefinition(invalidJson);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateTestDefinition_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var testRunner = new TestRunner();

        // Act
        var result = testRunner.ValidateTestDefinition("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetWelcomeMessage_ShouldReturnValidMessage()
    {
        // Arrange
        var testRunner = new TestRunner();

        // Act
        var message = testRunner.GetWelcomeMessage();

        // Assert
        Assert.NotNull(message);
        Assert.Contains("JTest", message);
        Assert.Contains("1.0.0", message);
    }
}