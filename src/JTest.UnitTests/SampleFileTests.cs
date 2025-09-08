using JTest.Core;

namespace JTest.UnitTests;

public class SampleFileTests
{

    [Fact]
    public void ValidateTestDefinition_WithTestSuite_ReturnsTrue()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = """
        {
            "version": "1.0",
            "tests": [
                {
                    "name": "Test case",
                    "steps": []
                }
            ]
        }
        """;

        // Act
        var isValid = testRunner.ValidateTestDefinition(testSuiteJson);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateTestDefinition_WithInvalidTestSuite_ReturnsFalse()
    {
        // Arrange
        var testRunner = new TestRunner();
        var invalidTestSuiteJson = """
        {
            "version": "1.0",
            "tests": [
                {
                    "name": "Test case"
                }
            ]
        }
        """;

        // Act
        var isValid = testRunner.ValidateTestDefinition(invalidTestSuiteJson);

        // Assert
        Assert.False(isValid);
    }
}