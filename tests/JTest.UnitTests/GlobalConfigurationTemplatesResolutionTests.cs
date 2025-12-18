using JTest.Core;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;
using System.Text.Json;

namespace JTest.UnitTests
{
    public sealed class GlobalConfigurationTemplatesResolutionTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _templateFilePath;
        private readonly string _explicitlyReferencedTemplateFilePath;
        private readonly string _templateSearchFolderPath;
        private readonly string _testFile;
        private readonly string _testFileWithExplicitUsings;

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

        private const string explicitReferencedTemplateFileContent = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "simple-wait",
                        "description": "Simple wait template for testing",
                        "params": {
                            "explicitWaitTime": { "type": "number", "required": false, "default": 10 }
                        },
                        "steps": [
                            {
                                "type": "wait",
                                "ms": "{{$.explicitWaitTime}}"
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

        private const string testFileWithExplicitUsingsContent = """
        {
            "version": "1.0",
            "info": {
                "name": "Global Templates Path Tests",
                "description": "Tests that globally configured template paths are resolved"
            },
            "using": [
                "test-explicit-wait-template.json"
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

        public GlobalConfigurationTemplatesResolutionTests()
        {
            // Create a temporary directory structure for testing
            _testDirectory = Path.Combine(Path.GetTempPath(), $"jtest_test_{Guid.NewGuid()}");
            _templateSearchFolderPath = Path.Combine(_testDirectory, "templates");
            var testsDir = Path.Combine(_testDirectory, "tests");

            Directory.CreateDirectory(_templateSearchFolderPath);
            Directory.CreateDirectory(testsDir);

            // Create template file
            _templateFilePath = Path.Combine(_templateSearchFolderPath, "test-simple-wait-template.json");
            File.WriteAllText(_templateFilePath, templateFileContent);

            // Create an explicitly referenced template file
            _explicitlyReferencedTemplateFilePath = Path.Combine(testsDir, "test-explicit-wait-template.json");            
            File.WriteAllText(_explicitlyReferencedTemplateFilePath, explicitReferencedTemplateFileContent);

            // Create a test file that references the templates implicitly with only global configuration
            _testFile = Path.Combine(testsDir, "global-test.json");
            File.WriteAllText(_testFile, testFileContent);

            // Create a test file that references the templates implicitly through global config and explicitly through "using" directive
            _testFileWithExplicitUsings = Path.Combine(testsDir, "explicit-using-test.json");
            File.WriteAllText(_testFileWithExplicitUsings, testFileWithExplicitUsingsContent);
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
            var testDoc = JsonDocument.Parse(testContent); 

            // Act
            var results = await testRunner.RunTestAsync(testDoc.RootElement, _testFile);

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
            var testDoc = JsonDocument.Parse(testContent);

            // Act
            var results = await testRunner.RunTestAsync(testDoc.RootElement, _testFile);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success, $"Test should pass but failed with error: {results[0].ErrorMessage}");
            Assert.Equal("Template resolution test", results[0].TestCaseName);
        }

        [Fact]
        public async Task RunTestAsync_With_ImplicitGlobalConfigurationTemplatePath_And_ExplicitTestSuiteUsingDirective_Then_ExplicitUsingsTakePrecedence()
        {
            // Arrange
            var globalConfiguration = new GlobalConfiguration(
                Templates: new(SearchPaths: [_templateSearchFolderPath], Paths: [])
            );
            var templateProvider = new Core.Templates.TemplateCollection();
            var testRunner = new TestRunner(templateProvider, globalConfiguration);
            var testContent = await File.ReadAllTextAsync(_testFileWithExplicitUsings);
            var testDoc = JsonDocument.Parse(testContent);

            // Act
            _ = await testRunner.RunTestAsync(testDoc.RootElement, _testFileWithExplicitUsings);

            // Assert
            Assert.Equal(1, templateProvider.Count);

            var template = templateProvider.GetTemplate("simple-wait");
            Assert.NotNull(template);
            Assert.True(template.Params?.ContainsKey("explicitWaitTime"));
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
