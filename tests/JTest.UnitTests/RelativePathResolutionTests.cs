using JTest.Core;
using System.IO;
using System.Text.Json;
using Xunit;

namespace JTest.UnitTests;

public class RelativePathResolutionTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _templateFile;
    private readonly string _testFile;

    public RelativePathResolutionTests()
    {
        // Create a temporary directory structure for testing
        _testDirectory = Path.Combine(Path.GetTempPath(), $"jtest_test_{Guid.NewGuid()}");
        
        var templatesDir = Path.Combine(_testDirectory, "templates");
        var testsDir = Path.Combine(_testDirectory, "tests");
        
        Directory.CreateDirectory(templatesDir);
        Directory.CreateDirectory(testsDir);

        // Create a template file
        _templateFile = Path.Combine(templatesDir, "test-templates.json");
        var templateContent = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "simple-wait",
                        "description": "Simple wait template for testing",
                        "params": {
                            "waitTime": { "type": "number", "required": false, "default": 10 }
                        },
                        "steps": [
                            {
                                "type": "wait",
                                "ms": "{{$.waitTime}}"
                            }
                        ],
                        "output": {
                            "result": "Wait completed successfully"
                        }
                    }
                ]
            }
        }
        """;
        File.WriteAllText(_templateFile, templateContent);

        // Create a test file that references the template with a relative path
        _testFile = Path.Combine(testsDir, "relative-path-test.json");
        var testContent = """
        {
            "version": "1.0",
            "info": {
                "name": "Relative Path Test",
                "description": "Test that relative template paths are resolved relative to test file"
            },
            "using": [
                "../templates/test-templates.json"
            ],
            "tests": [
                {
                    "name": "Template resolution test",
                    "description": "Uses a template with relative path",
                    "steps": [
                        {
                            "type": "use",
                            "template": "simple-wait",
                            "with": {
                                "waitTime": 1
                            }
                        }
                    ]
                }
            ]
        }
        """;
        File.WriteAllText(_testFile, testContent);
    }

    [Fact]
    public async Task RunTestAsync_WithRelativeTemplatePath_ResolvesRelativeToTestFile()
    {
        // Arrange
        var testRunner = new TestRunner();
        var testContent = await File.ReadAllTextAsync(_testFile);

        // Act - Run test from a different working directory (the parent directory)
        // This should still work because paths are resolved relative to the test file, not the working directory
        var originalWorkingDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory); // Change to parent directory
            var testDoc = JsonDocument.Parse(testContent);
            var results = await testRunner.RunTestAsync(testDoc.RootElement, _testFile);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success, $"Test should pass but failed with error: {results[0].ErrorMessage}");
            Assert.Equal("Template resolution test", results[0].TestCaseName);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalWorkingDirectory);
        }
    }

    [Fact]
    public async Task RunTestAsync_WithRelativeTemplatePath_FromDifferentWorkingDirectory_StillWorks()
    {
        // Arrange  
        var testRunner = new TestRunner();
        var testContent = await File.ReadAllTextAsync(_testFile);
        
        // Act - Run test from an entirely different working directory
        var originalWorkingDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(Path.GetTempPath()); // Change to a completely different directory
            var testDoc = JsonDocument.Parse(testContent);
            var results = await testRunner.RunTestAsync(testDoc.RootElement, _testFile);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success, $"Test should pass but failed with error: {results[0].ErrorMessage}");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalWorkingDirectory);
        }
    }

    public void Dispose()
    {
        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}