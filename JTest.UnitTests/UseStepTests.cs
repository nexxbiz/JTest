using System.Text.Json;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;
using Xunit;

namespace JTest.UnitTests;

public class UseStepTests
{
    [Fact]
    public void UseStep_ValidateConfiguration_RequiresTemplateProperty()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var configWithoutTemplate = JsonSerializer.Deserialize<JsonElement>("{}");
        var configWithTemplate = JsonSerializer.Deserialize<JsonElement>("{\"template\": \"test\"}");

        // Act & Assert
        Assert.False(useStep.ValidateConfiguration(configWithoutTemplate));
        Assert.True(useStep.ValidateConfiguration(configWithTemplate));
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_ThrowsWhenTemplateNotFound()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var config = JsonSerializer.Deserialize<JsonElement>("{\"template\": \"nonexistent\"}");
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();

        // Act & Assert
        var result = await useStep.ExecuteAsync(context);
        Assert.False(result.Success);
        Assert.Contains("Template 'nonexistent' not found", result.ErrorMessage);
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_ExecutesTemplateWithParameters()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        
        // Load test template
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "testParam": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "result": "Template executed with {{$.testParam}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {
                "testParam": "hello world"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("result", context.Variables.Keys);
        Assert.Equal("Template executed with hello world", context.Variables["result"]);
        
        // Check output prefix access pattern
        Assert.Contains("output", context.Variables.Keys);
        var outputDict = Assert.IsType<Dictionary<string, object>>(context.Variables["output"]);
        Assert.Equal("Template executed with hello world", outputDict["result"]);
    }

    [Fact] 
    public async Task UseStep_ExecuteAsync_ValidatesRequiredParameters()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        
        // Load template with required parameter
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "requiredParam": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {}
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {}
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Required template parameter 'requiredParam' not provided", result.ErrorMessage);
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_UsesDefaultParameterValues()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        
        // Load template with default parameter
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "optionalParam": { "type": "string", "required": false, "default": "default value" }
                        },
                        "steps": [],
                        "output": {
                            "result": "{{$.optionalParam}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {}
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("default value", context.Variables["result"]);
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_ResolvesParameterTokensFromParentContext()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "testParam": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "result": "{{$.testParam}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {
                "testParam": "{{$.env.baseUrl}}"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();
        context.Variables["env"] = new Dictionary<string, object>
        {
            ["baseUrl"] = "https://api.example.com"
        };

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("https://api.example.com", context.Variables["result"]);
    }
}

public class TemplateProviderTests
{
    [Fact]
    public void TemplateProvider_LoadTemplatesFromJson_LoadsValidTemplates()
    {
        // Arrange
        var provider = new TemplateProvider();
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "template1",
                        "steps": [],
                        "output": {}
                    },
                    {
                        "name": "template2",
                        "steps": [],
                        "output": {}
                    }
                ]
            }
        }
        """;

        // Act
        provider.LoadTemplatesFromJson(templateJson);

        // Assert
        Assert.Equal(2, provider.Count);
        Assert.NotNull(provider.GetTemplate("template1"));
        Assert.NotNull(provider.GetTemplate("template2"));
        Assert.Null(provider.GetTemplate("nonexistent"));
    }

    [Fact]
    public void TemplateProvider_RegisterTemplateCollection_RegistersTemplates()
    {
        // Arrange
        var provider = new TemplateProvider();
        var collection = new TemplateCollection
        {
            Components = new TemplateComponents
            {
                Templates = new List<Template>
                {
                    new Template { Name = "test-template" }
                }
            }
        };

        // Act
        provider.RegisterTemplateCollection(collection);

        // Assert
        Assert.Equal(1, provider.Count);
        Assert.NotNull(provider.GetTemplate("test-template"));
    }

    [Fact]
    public void TemplateProvider_Clear_RemovesAllTemplates()
    {
        // Arrange
        var provider = new TemplateProvider();
        var collection = new TemplateCollection
        {
            Components = new TemplateComponents
            {
                Templates = new List<Template>
                {
                    new Template { Name = "test-template" }
                }
            }
        };
        provider.RegisterTemplateCollection(collection);

        // Act
        provider.Clear();

        // Assert
        Assert.Equal(0, provider.Count);
        Assert.Null(provider.GetTemplate("test-template"));
    }
}