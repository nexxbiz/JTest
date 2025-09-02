using JTest.Core;
using Xunit;

namespace JTest.UnitTests;

public class SampleFileTests
{
    [Fact]
    public async Task RunTestAsync_WithActualSampleFile_ParsesCorrectly()
    {
        // Arrange
        var testRunner = new TestRunner();
        
        // This is the exact structure from the problem statement
        var testSuiteJson = """
        {
            "version": "1.0",
            "info": {
                "name": "if-else-tests",
                "description": "testing the full set of if else capabilities"
            },
            "using": [
                "./elsa-templates.json"
            ],
            "env": {
                "tokenUrl": "https://toolbox-executor-dev-designer-api.nexxbiz.tech/elsa/api/identity/login",
                "username": "nexxbizadmin",
                "password": "yQ33Eyha9kBNrD"
            },
            "globals": {
                "token": null,
                "authHeader": null
            },
            "tests": [
                {
                    "name": "Authenticate and store credentials",
                    "description": "Call authentication API using template and store credentials globally",
                    "steps": [
                        {
                            "type": "wait",
                            "ms": 100
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var results = await testRunner.RunTestAsync(testSuiteJson);

        // Assert
        Assert.Single(results);
        Assert.Equal("Authenticate and store credentials", results[0].TestCaseName);
        Assert.True(results[0].Success);
        Assert.Single(results[0].StepResults);
    }

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