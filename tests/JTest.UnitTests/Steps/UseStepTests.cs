using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.UnitTests.TestHelpers;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;

namespace JTest.UnitTests.Steps;

public class UseStepTests
{
    [Fact]
    public void When_Validate_WithInvalidConfig_ReturnsFalse()
    {
        // Arrange
        const string unknownTemplateName = "unknown-template";
        var context = new TestExecutionContext();
        var step = GetSut(unknownTemplateName);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.NotEmpty(errors);
    } 

    [Fact]
    public async Task UseStep_ExecuteAsync_ExecutesTemplateWithParameters()
    {
        // Arrange
        const string templateJson = """
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
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext
            .GetTemplate(Arg.Any<string>())
            .Returns(template);        

        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "test-template",
                "with": {
                    "testParam": "hello world"
                }
            }
            """,
            JsonSerializerHelper.Options
        )!;
        
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("result"));
        Assert.NotNull(result.Data["result"]);
        Assert.Equal("Template executed with hello world", result.Data["result"]);
    }

    [Fact]
    public async Task When_ExecuteAsync_And_RequiredParametersMissingValue_Then_ThrowsException()
    {
        // Arrange
        const string templateJson = """
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
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "test-template",
                "with": {}
            }
            """,
            JsonSerializerHelper.Options
        )!;
        
        var context = new TestExecutionContext();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>_ = await useStep.ExecuteAsync(context)
        );
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_UsesDefaultParameterValues()
    {
        // Arrange
        var templateProvider = new Core.Templates.TemplateCollection();
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

        
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {}
        }
        """);
        var useStep = new UseStep(templateProvider, stepFactory, config);

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
        var templateProvider = new Core.Templates.TemplateCollection();
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

        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "template": "test-template",
            "with": {
                "testParam": "{{$.env.baseUrl}}"
            }
        }
        """);
        var useStep = new UseStep(templateProvider, stepFactory, config);

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
        var templateProvider = new Core.Templates.TemplateCollection();
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
        var useStep = new UseStep(templateProvider, stepFactory, config);

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


    static UseStep GetSut(string templateName, IReadOnlyDictionary<string, object?>? with = null, ITemplateContext? context = null, IServiceProvider? serviceProvider = null)
    {
        return new UseStep(
            Substitute.For<IAnsiConsole>(),
            context ?? Substitute.For<ITemplateContext>(),
            StepProcessor.Default,
            serviceProvider ?? Substitute.For<IServiceProvider>(),
            new UseStepConfiguration(Template: templateName, With: with)
        );
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
            var templateProvider = new Core.Templates.TemplateCollection();

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

            // Configure UseStep to use the template
            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "case-data-template"
            }));

            // Create UseStep
            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider), configuration);

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
            var templateProvider = new Core.Templates.TemplateCollection();

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


            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "simple-template"
            }));

            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider), configuration);

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
            var templateProvider = new Core.Templates.TemplateCollection();

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

            var configuration = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
            {
                type = "use",
                template = "mixed-template",
                @with = new
                {
                    baseUrl = "https://api.example.com"
                }
            }));

            var useStep = new TestUseStep(templateProvider, new TestStepFactory(templateProvider), configuration);

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