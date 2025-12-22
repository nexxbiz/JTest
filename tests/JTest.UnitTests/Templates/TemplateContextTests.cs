using JTest.Core.Models;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.UnitTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;

namespace JTest.UnitTests.Templates;

public sealed class TemplateContextTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _templateFilePath;
    private readonly string _explicitlyReferencedTemplateFilePath;
    private readonly string _templateSearchFolderPath;
    private readonly string _templatesSearchFolderPattern;
    private readonly string _testFile;
    private readonly string _testFileWithExplicitUsings;

    public TemplateContextTests()
    {
        // Create a temporary directory structure for testing
        var workingDirectory = Directory.GetCurrentDirectory();
        var testDirectoryName = $"test_{Guid.NewGuid()}";
        _testDirectory = Path.Combine(workingDirectory, testDirectoryName);
        _templateSearchFolderPath = Path.Combine(_testDirectory, "templates");
        _templatesSearchFolderPattern = Path.Combine(testDirectoryName, "templates", "*");
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
    public async Task When_Load_Then_ReadsGlobalTemplates()
    {
        // Arrange
        var globalConfiguration = new GlobalConfiguration(
            Templates: new(SearchPaths: [], Paths: [_templateFilePath])
        );
        var templateContext = GetSut(globalConfiguration);
        var testSuite = new JTestSuite
        {
            Using = []
        };

        // Act
        await templateContext.Load(testSuite);

        // Assert
        var template = templateContext.GetTemplate("simple-wait");
        Assert.NotNull(template);
        Assert.Equal("simple-wait", template.Name);
    }


    [Fact]
    public async Task When_Load_Then_LoadsTemplates_Specified_In_TestSuite()
    {
        // Arrange
        var templateContext = GetSut(globalConfiguration: null);
        var testSuite = new JTestSuite
        {
            Using = [
                _templateFilePath
            ]
        };

        // Act
        await templateContext.Load(testSuite);

        // Assert
        var template = templateContext.GetTemplate("simple-wait");
        Assert.NotNull(template);
        Assert.Equal("simple-wait", template.Name);
    }



    [Fact]
    public async Task RunTestAsync_With_ImplicitGlobalConfigurationTemplatePath_And_ExplicitTestSuiteUsingDirective_Then_ExplicitUsingsTakePrecedence()
    {
        // Arrange
        var globalConfiguration = new GlobalConfiguration(
            Templates: new(SearchPaths: [_templatesSearchFolderPattern], Paths: [])
        );
        var templateContext = GetSut(globalConfiguration);
        var testSuite = JsonSerializer.Deserialize<JTestSuite>(
            File.ReadAllText(_testFileWithExplicitUsings),
            JsonSerializerHelper.Options
        );
        testSuite!.FilePath = _testFileWithExplicitUsings;

        // Act
        await templateContext.Load(testSuite!);

        // Assert            
        var template = templateContext.GetTemplate("simple-wait");
        Assert.NotNull(template);
        Assert.True(template.Params?.ContainsKey("explicitWaitTime"));
    }

    private static TemplateContext GetSut(GlobalConfiguration? globalConfiguration = null)
    {
        var globalConfigAccessor = Substitute.For<IGlobalConfigurationAccessor>();
        globalConfigAccessor.Configuration.Returns(globalConfiguration ?? new());

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        return new TemplateContext(
            Substitute.For<IAnsiConsole>(),
            Substitute.For<HttpClient>(),
            globalConfigAccessor,
            new JsonSerializerOptionsAccessor(serviceProvider)
        );
    }

    public void Dispose()
    {
        // Cleanup
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

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
                            "ms": "{{ $.waitTime }}"
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
}
