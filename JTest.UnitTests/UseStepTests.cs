using System.Text.Json;
using JTest.Core.Debugging;
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
        
        // Template outputs should be accessible via {{$.this.outputKey}} pattern
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("Template executed with hello world", thisResult["result"]);
        Assert.Equal("template", thisResult["type"]);
        
        // Direct output access should NOT be available
        Assert.DoesNotContain("result", context.Variables.Keys);
        
        // Old output prefix access should NOT be available
        Assert.DoesNotContain("output", context.Variables.Keys);
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
        
        // Template outputs should be accessible via {{$.this.outputKey}} pattern
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("default value", thisResult["result"]);
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
        
        // Template outputs should be accessible via {{$.this.outputKey}} pattern
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("https://api.example.com", thisResult["result"]);
    }
    
    [Fact]
    public async Task UseStep_ExecuteAsync_ProcessesSaveOperations()
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
                        "name": "auth-template",
                        "params": {
                            "username": { "type": "string", "required": true },
                            "password": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "token": "{{$.username}}-{{$.password}}-token",
                            "authHeader": "Bearer {{$.username}}-{{$.password}}-token"
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
            "template": "auth-template",
            "with": {
                "username": "testuser",
                "password": "testpass"
            },
            "save": {
                "$.globals.token": "{{$.this.token}}",
                "$.globals.authHeader": "{{$.this.authHeader}}"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();
        context.Variables["globals"] = new Dictionary<string, object>();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        
        // Template outputs should be accessible via {{$.this.outputKey}} pattern
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("testuser-testpass-token", thisResult["token"]);
        Assert.Equal("Bearer testuser-testpass-token", thisResult["authHeader"]);
        
        // Save operations should have stored values in globals
        Assert.Contains("globals", context.Variables.Keys);
        var globals = Assert.IsType<Dictionary<string, object>>(context.Variables["globals"]);
        Assert.Equal("testuser-testpass-token", globals["token"]);
        Assert.Equal("Bearer testuser-testpass-token", globals["authHeader"]);
    }

    [Fact]
    public async Task UseStep_WithDebugLogger_GeneratesTemplateExecutionDetails()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        var debugLogger = new MarkdownDebugLogger();
        
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "apiUrl": { "type": "string", "required": true },
                            "timeout": { "type": "number", "required": false, "default": 30 }
                        },
                        "steps": [],
                        "output": {
                            "status": "complete",
                            "endpoint": "{{$.apiUrl}}/api"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory, debugLogger);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {
                "apiUrl": "https://api.example.com"
            },
            "save": {
                "$.globals.endpoint": "{{$.this.endpoint}}"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();
        context.Variables["globals"] = new Dictionary<string, object>();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        
        var output = debugLogger.GetOutput();
        
        // Verify template execution details are included in collapsible section
        Assert.Contains("<details>", output);
        Assert.Contains("<summary>Template Execution Details (Click to expand)</summary>", output);
        Assert.Contains("**Template:** test-template", output);
        Assert.Contains("**Steps Executed:** 0", output);
        Assert.Contains("</details>", output);
        
        // Verify input parameters are shown
        Assert.Contains("**Input Parameters:**", output);
        Assert.Contains("- `apiUrl`: \"https://api.example.com\"", output);
        
        // Verify template outputs are shown
        Assert.Contains("**Template Outputs:**", output);
        Assert.Contains("- `status`: \"complete\"", output);
        Assert.Contains("- `endpoint`: \"https://api.example.com/api\"", output);
        
        // Verify saved variables are shown
        Assert.Contains("**Variables Saved:**", output);
        Assert.Contains("- `$.globals.endpoint`: \"https://api.example.com/api\"", output);
        
        // Print the complete debug output for verification
        Console.WriteLine("=== TEMPLATE EXECUTION DEBUG OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");
    }

    [Fact]
    public async Task UseStep_WithComplexTemplate_ShowsAllExecutionDetails()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        var debugLogger = new MarkdownDebugLogger();
        
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "complex-template",
                        "params": {
                            "baseUrl": { "type": "string", "required": true },
                            "retries": { "type": "number", "required": false, "default": 3 },
                            "timeout": { "type": "number", "required": false, "default": 30 }
                        },
                        "steps": [
                            {
                                "type": "wait",
                                "ms": 1
                            }
                        ],
                        "output": {
                            "finalUrl": "{{$.baseUrl}}/api",
                            "retryCount": "{{$.retries}}",
                            "timeoutMs": "{{$.timeout}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory, debugLogger);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "complex-template",
            "with": {
                "baseUrl": "https://complex.api.com"
            },
            "save": {
                "$.globals.finalUrl": "{{$.this.finalUrl}}",
                "$.globals.retries": "{{$.this.retryCount}}"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();
        context.Variables["globals"] = new Dictionary<string, object>();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "Unknown error");
        
        var output = debugLogger.GetOutput();
        
        // Verify template execution details show steps were executed in collapsible format
        Assert.Contains("<details>", output);
        Assert.Contains("<summary>Template Execution Details (Click to expand)</summary>", output);
        Assert.Contains("**Template:** complex-template", output);
        Assert.Contains("**Steps Executed:** 1", output);
        Assert.Contains("</details>", output);
        
        // Verify input parameters including default values
        Assert.Contains("**Input Parameters:**", output);
        Assert.Contains("- `baseUrl`: \"https://complex.api.com\"", output);
        
        // Verify template outputs with computed values
        Assert.Contains("**Template Outputs:**", output);
        Assert.Contains("- `finalUrl`: \"https://complex.api.com/api\"", output);
        Assert.Contains("- `retryCount`: 3", output);
        Assert.Contains("- `timeoutMs`: 30", output);
        
        // Verify multiple saved variables
        Assert.Contains("**Variables Saved:**", output);
        Assert.Contains("- `$.globals.finalUrl`: \"https://complex.api.com/api\"", output);
        Assert.Contains("- `$.globals.retries`: 3", output);
        
        // Verify the actual saved values in context
        var globals = Assert.IsType<Dictionary<string, object>>(context.Variables["globals"]);
        Assert.Equal("https://complex.api.com/api", globals["finalUrl"]);
        Assert.Equal(3, globals["retries"]);
        
        // Print the complete debug output for verification
        Console.WriteLine("=== COMPLEX TEMPLATE DEBUG OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine("=== END OUTPUT ===");
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

    [Fact]
    public async Task UseStep_WithNestedTemplates_ShowsCollapsibleDetailsForBoth()
    {
        // Arrange
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        var debugLogger = new MarkdownDebugLogger();
        
        var templatesJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "inner-template",
                        "params": {
                            "value": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "result": "processed-{{$.value}}"
                        }
                    },
                    {
                        "name": "outer-template",
                        "params": {
                            "input": { "type": "string", "required": true }
                        },
                        "steps": [
                            {
                                "type": "use",
                                "template": "inner-template",
                                "with": {
                                    "value": "{{$.input}}"
                                }
                            }
                        ],
                        "output": {
                            "final": "{{$.this.result}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templatesJson);
        
        stepFactory.SetDebugLogger(debugLogger);
        var useStep = new UseStep(templateProvider, stepFactory, debugLogger);
        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "use",
            "template": "outer-template",
            "with": {
                "input": "test-data"
            }
        }
        """);
        useStep.ValidateConfiguration(config);
        
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        
        var output = debugLogger.GetOutput();
        
        // Should have multiple collapsible details sections for nested templates
        var detailsCount = output.Split("<details>").Length - 1;
        Assert.True(detailsCount >= 2, $"Expected at least 2 details sections but found {detailsCount}");
        
        // Should contain both template executions in collapsible format
        Assert.Contains("<summary>Template Execution Details (Click to expand)</summary>", output);
        Assert.Contains("**Template:** outer-template", output);
        Assert.Contains("**Template:** inner-template", output);
        
        // Print the complete debug output for analysis
        Console.WriteLine("=== NESTED TEMPLATE DEBUG OUTPUT ===");
        Console.WriteLine(output);
        Console.WriteLine($"=== Details sections found: {detailsCount} ===");
        Console.WriteLine("=== END OUTPUT ===");
    }
}