using JTest.Core;
using JTest.Core.Models;

namespace JTest.UnitTests
{
    public class GlobalConfigurationTemplatesResolutionTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _templateFilePath;
        private readonly string _templateSearchFolderPath;
        private readonly string _testFile;

        private const string templateFileContent = """
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
        private const string testFileContent = """
        {
            "version": "1.0",
            "info": {
                "name": "Global Templates Path Tests",
                "description": "Tests that globally configured template paths are resolved"
            },
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

        public GlobalConfigurationTemplatesResolutionTests()
        {
            // Create a temporary directory structure for testing
            _testDirectory = Path.Combine(Path.GetTempPath(), $"jtest_test_{Guid.NewGuid()}");
            _templateSearchFolderPath = Path.Combine(_testDirectory, "templates");
            var testsDir = Path.Combine(_testDirectory, "tests");

            Directory.CreateDirectory(_templateSearchFolderPath);
            Directory.CreateDirectory(testsDir);

            // Create first template file
            _templateFilePath = Path.Combine(_templateSearchFolderPath, "test-templates.json");            
            File.WriteAllText(_templateFilePath, templateFileContent);
            
            // Create a test file that references the templates without using
            _testFile = Path.Combine(testsDir, "relative-path-test.json");
            File.WriteAllText(_testFile, testFileContent);
        }

        [Fact]
        public async Task RunTestAsync_WithGlobalConfigurationTemplatePath_ResolvesTemplatePathForTestCase()
        {
            // Arrange
            var globalConfiguration = new GlobalConfiguration(
                Templates: new(SearchPaths: [], Paths: [_templateFilePath])
            );
            var testRunner = new TestRunner(globalConfiguration);
            var testContent = await File.ReadAllTextAsync(_testFile);

            // Act
            var results = await testRunner.RunTestAsync(testContent, _testFile);
                
            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success, $"Test should pass but failed with error: {results[0].ErrorMessage}");
            Assert.Equal("Template resolution test", results[0].TestCaseName);
        }

        [Fact]
        public async Task RunTestAsync_WithGlobalConfigurationTemplateSearchPath_ResolvesTemplatePathForTestCase()
        {
            // Arrange
            var globalConfiguration = new GlobalConfiguration(
                Templates: new(SearchPaths: [_templateSearchFolderPath], Paths: [])
            );
            var testRunner = new TestRunner(globalConfiguration);
            var testContent = await File.ReadAllTextAsync(_testFile);

            // Act
            var results = await testRunner.RunTestAsync(testContent, _testFile);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success, $"Test should pass but failed with error: {results[0].ErrorMessage}");
            Assert.Equal("Template resolution test", results[0].TestCaseName);
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
                var results = await testRunner.RunTestAsync(testContent, _testFile);

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
}
