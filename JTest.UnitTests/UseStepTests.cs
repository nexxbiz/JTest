using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JTest.UnitTests;

/// <summary>
/// Test step factory that creates mocked HttpStep with debug logging for testing
/// </summary>
public class TestStepFactory : StepFactory
{


    public TestStepFactory(ITemplateProvider templateProvider)
        : base(templateProvider)
    {

    }

    public override IStep CreateStep(object stepConfig)
    {
        var json = JsonSerializer.Serialize(stepConfig);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        if (!jsonElement.TryGetProperty("type", out var typeElement))
        {
            throw new ArgumentException("Step configuration must have a 'type' property");
        }

        var stepType = typeElement.GetString();

        IStep step = stepType?.ToLowerInvariant() switch
        {
            "http" => CreateMockedHttpStep(),
            "wait" => new WaitStep(),
            "use" => new TestUseStep(TemplateProvider, this),
            _ => throw new ArgumentException($"Unknown step type: {stepType}")
        };

        // Set step ID if provided
        if (jsonElement.TryGetProperty("id", out var idElement))
        {
            step.Id = idElement.GetString();
        }

        // Validate configuration
        if (!step.ValidateConfiguration(jsonElement))
        {
            throw new ArgumentException($"Invalid configuration for step type '{stepType}'");
        }

        return step;
    }

    private HttpStep CreateMockedHttpStep()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\",\"statusCode\":200}", Encoding.UTF8, "application/json")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object);
        return new HttpStep(httpClient);
    }
}

/// <summary>
/// Test UseStep that can use a custom step factory for template execution
/// </summary>
public class TestUseStep : UseStep
{
    private readonly StepFactory _testStepFactory;

    public TestUseStep(ITemplateProvider templateProvider, StepFactory stepFactory)
        : base(templateProvider, stepFactory)
    {
        _testStepFactory = stepFactory;
    }


}

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



    /// <summary>
    /// Tests for UseStep case data variable access functionality
    /// These tests verify that templates can access case data variables for data-driven testing
    /// </summary>
    public class UseStepCaseDataTests
    {
        [Fact]
        public async Task UseStep_WithCaseData_CanAccessCaseVariablesInTemplate()
        {
            // Arrange - Create a template that uses case data
            var templateProvider = new TemplateProvider();

            var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "case-data-template",
                        "params": {},
                        "steps": [
                            {
                                "type": "wait",
                                "ms": 1,
                                "id": "test-step"
                            }
                        ],
                        "output": {
                            "userId": "{{$.case.userId}}",
                            "accountId": "{{$.case.accountId}}",
                            "expectedTotal": "{{$.case.expectedTotal}}"
                        }
                    }
                ]
            }
        }
        """;
            templateProvider.LoadTemplatesFromJson(templateJson);

            // Create UseStep
            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider));

            // Configure UseStep to use the template
            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "case-data-template"
            }));

            useStep.ValidateConfiguration(configuration);

            // Create execution context with case data
            var context = new TestExecutionContext();
            var caseData = new Dictionary<string, object>
            {
                ["userId"] = "user123",
                ["accountId"] = "acct-1001",
                ["expectedTotal"] = 25.50
            };
            context.SetCase(caseData);

            // Act
            var result = await useStep.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var resultDict = result.Data as Dictionary<string, object>;
            Assert.NotNull(resultDict);

            // Verify template outputs correctly resolved case variables
            Assert.Equal("user123", resultDict["userId"]);
            Assert.Equal("acct-1001", resultDict["accountId"]);
            Assert.Equal(25.5, resultDict["expectedTotal"]); // Numbers should preserve type
        }

        [Fact]
        public async Task UseStep_WithoutCaseData_WorksNormally()
        {
            // Arrange - Create a simple template that doesn't use case data
            var templateProvider = new TemplateProvider();

            var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "simple-template",
                        "params": {},
                        "steps": [
                            {
                                "type": "wait",
                                "ms": 1
                            }
                        ],
                        "output": {
                            "message": "Template executed successfully"
                        }
                    }
                ]
            }
        }
        """;
            templateProvider.LoadTemplatesFromJson(templateJson);

            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider));

            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "simple-template"
            }));

            useStep.ValidateConfiguration(configuration);

            // Create execution context WITHOUT case data
            var context = new TestExecutionContext();

            // Act
            var result = await useStep.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var resultDict = result.Data as Dictionary<string, object>;
            Assert.NotNull(resultDict);
            Assert.Equal("Template executed successfully", resultDict["message"]);
        }

        [Fact]
        public async Task UseStep_WithCaseDataAndTemplateParams_BothAccessible()
        {
            // Arrange - Template that uses both case data and template parameters
            var templateProvider = new TemplateProvider();

            var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "mixed-template",
                        "params": {
                            "baseUrl": { "type": "string", "required": true }
                        },
                        "steps": [
                            {
                                "type": "wait",
                                "ms": 1
                            }
                        ],
                        "output": {
                            "fullUrl": "{{$.baseUrl}}/users/{{$.case.userId}}",
                            "userInfo": "{{$.case.userId}}"
                        }
                    }
                ]
            }
        }
        """;
            templateProvider.LoadTemplatesFromJson(templateJson);

            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider));

            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "mixed-template",
                @with = new
                {
                    baseUrl = "https://api.example.com"
                }
            }));

            useStep.ValidateConfiguration(configuration);

            // Create execution context with case data
            var context = new TestExecutionContext();
            var caseData = new Dictionary<string, object>
            {
                ["userId"] = "user456"
            };
            context.SetCase(caseData);

            // Act
            var result = await useStep.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var resultDict = result.Data as Dictionary<string, object>;
            Assert.NotNull(resultDict);

            // Verify both template parameters and case data are accessible
            Assert.Equal("https://api.example.com/users/user456", resultDict["fullUrl"]);
            Assert.Equal("user456", resultDict["userInfo"]);
        }
    }
}