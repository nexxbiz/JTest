using JTest.Core;

namespace JTest.UnitTests;

public class UsingStatementTests
{

    [Fact]
    public async Task RunTestAsync_WithNonExistentTemplateFile_ThrowsException()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = """
        {
            "version": "1.0",
            "using": [
                "/nonexistent/path/templates.json"
            ],
            "tests": [
                {
                    "name": "Test with bad template path",
                    "steps": [
                        {
                            "type": "wait",
                            "ms": 10
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRunner.RunTestAsync(testSuiteJson));
    }

    [Fact]
    public async Task RunTestAsync_WithEmptyUsingArray_ExecutesNormally()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = """
        {
            "version": "1.0",
            "using": [],
            "tests": [
                {
                    "name": "Test with empty using",
                    "steps": [
                        {
                            "type": "wait",
                            "ms": 10
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
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task RunTestAsync_WithNullUsing_ExecutesNormally()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = """
        {
            "version": "1.0",
            "tests": [
                {
                    "name": "Test without using",
                    "steps": [
                        {
                            "type": "wait",
                            "ms": 10
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
        Assert.True(results[0].Success);
    }


    [Fact]
    public async Task RunTestAsync_WithInvalidHttpUrl_ThrowsException()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = """
        {
            "version": "1.0",
            "using": [
                "https://nonexistent.invalid.url/templates.json"
            ],
            "tests": [
                {
                    "name": "Test with invalid HTTP URL",
                    "steps": [
                        {
                            "type": "wait",
                            "ms": 10
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRunner.RunTestAsync(testSuiteJson));
    }
}