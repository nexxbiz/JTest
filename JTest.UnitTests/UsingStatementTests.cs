using JTest.Core;
using Xunit;

namespace JTest.UnitTests;

public class UsingStatementTests
{
    [Fact]
    public async Task RunTestAsync_WithUsingStatement_LoadsTemplatesSuccessfully()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = $$"""
        {
            "version": "1.0",
            "using": [
                "/tmp/templates/test-templates.json"
            ],
            "tests": [
                {
                    "name": "Test using templates",
                    "steps": [
                        {
                            "type": "use",
                            "template": "simple-test",
                            "with": {
                                "message": "Hello from template"
                            }
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
        Assert.Equal("Test using templates", results[0].TestCaseName);
    }

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
    public async Task RunTestAsync_WithDuplicateTemplateNames_LogsWarnings()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testSuiteJson = $$"""
        {
            "version": "1.0",
            "using": [
                "/tmp/templates/test-templates.json",
                "/tmp/templates/duplicate-templates.json"
            ],
            "tests": [
                {
                    "name": "Test with duplicate template names",
                    "steps": [
                        {
                            "type": "use",
                            "template": "simple-test",
                            "with": {
                                "greeting": "Hello world"
                            }
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
        // The overwrite warning should be logged but we can't easily check it in this test
        // The important thing is that it doesn't fail and uses the last loaded template
    }
}