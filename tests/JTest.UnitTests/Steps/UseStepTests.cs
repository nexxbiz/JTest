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

public sealed class UseStepTests
{
    [Fact]
    public void When_Validate_And_ConfigurationInvalid_ReturnsFalse()
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
            "name": "test-template",
            "params": {
                "testParam": { "type": "string", "required": true }
            },
            "steps": [],
            "output": {
                "result": "Template executed with {{$.testParam}}"
            }
        }
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext
            .GetTemplate(Arg.Is("test-template"))
            .Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);

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
            serializerOptions
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
            "name": "test-template",
            "params": {
                "requiredParam": { "type": "string", "required": true }
            },
            "steps": [],
            "output": {}
        }
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("test-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "test-template",
                "with": {}
            }
            """,
            serializerOptions
        )!;

        var context = new TestExecutionContext();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => _ = await useStep.ExecuteAsync(context)
        );
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_UsesDefaultParameterValues()
    {
        // Arrange
        const string templateJson = """
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
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("test-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "test-template",
                "with": {}
            }
            """,
            serializerOptions
        )!;
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("default value", $"{result.Data["result"]}");
    }

    [Fact]
    public async Task UseStep_ExecuteAsync_ResolvesParameterTokensFromParentContext()
    {
        // Arrange
        const string templateJson = """
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
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("test-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "test-template",
                "with": {
                    "testParam": "{{$.env.baseUrl}}"
                }
            }
            """,
            serializerOptions
        )!;
        var context = new TestExecutionContext();
        context.Variables["env"] = new Dictionary<string, object?>
        {
            ["baseUrl"] = "https://api.example.com"
        };

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("https://api.example.com", result.Data["result"]);
    }


    [Fact]
    public async Task UseStep_WithCaseData_CanAccessCaseVariablesInTemplate()
    {
        // Arrange - Create a template that uses case data        
        const string templateJson = """
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
        """;

        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("case-data-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "case-data-template"
            }
            """,
            serializerOptions
        )!;

        // Create execution context with case data
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object?>
        {
            ["userId"] = "user123",
            ["accountId"] = "acct-1001",
            ["expectedTotal"] = 25.50
        };
        context.SetCase(caseData);

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert        
        Assert.NotNull(result.Data);

        // Verify template outputs correctly resolved case variables
        Assert.Equal("user123", result.Data["userId"]);
        Assert.Equal("acct-1001", result.Data["accountId"]);
        Assert.Equal(25.5, result.Data["expectedTotal"]); // Numbers should preserve type
    }

    [Fact]
    public async Task UseStep_WithoutCaseData_WorksNormally()
    {
        // Arrange - Create a simple template that doesn't use case data        
        const string templateJson = """
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
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("simple-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "simple-template"
            }
            """,
            serializerOptions
        )!;

        // Create execution context WITHOUT case data
        var context = new TestExecutionContext();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("Template executed successfully", result.Data["message"]);
    }

    [Fact]
    public async Task UseStep_WithCaseDataAndTemplateParams_BothAccessible()
    {
        // Arrange - Template that uses both case data and template parameters        
        const string templateJson = """
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
        """;
        var template = JsonSerializer.Deserialize<Template>(templateJson, JsonSerializerHelper.Options);
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Is("mixed-template")).Returns(template);
        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(templateContext: templateContext);
        var useStep = JsonSerializer.Deserialize<IStep>(
            """
            {
                "type": "use",
                "template": "mixed-template",
                "with":{
                    "baseUrl": "https://api.example.com"
                }
            }
            """,
            serializerOptions
        )!;

        // Create execution context with case data
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object?>
        {
            ["userId"] = "user456"
        };
        context.SetCase(caseData);

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data);

        // Verify both template parameters and case data are accessible
        Assert.Equal("https://api.example.com/users/user456", result.Data["fullUrl"]);
        Assert.Equal("user456", result.Data["userInfo"]);
    }


    private static UseStep GetSut(string templateName, IReadOnlyDictionary<string, object?>? with = null, ITemplateContext? context = null, IServiceProvider? serviceProvider = null)
    {
        return new UseStep(
            Substitute.For<IAnsiConsole>(),
            context ?? Substitute.For<ITemplateContext>(),
            StepProcessor.Default,
            serviceProvider ?? Substitute.For<IServiceProvider>(),
            new UseStepConfiguration(Template: templateName, With: with)
        );
    }
}