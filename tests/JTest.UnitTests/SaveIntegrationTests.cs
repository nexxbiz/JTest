using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Templates;
using System.Text.Json;

namespace JTest.UnitTests;

public class SaveIntegrationTests
{
    [Fact]
    public async Task UseStep_SaveFunctionality_MatchesRequirementsExample()
    {
        // Arrange - create template matching the auth example from docs
        var templateProvider = new TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);

        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "authenticate",
                        "description": "Authenticate user and obtain access token",
                        "params": {
                            "username": { "type": "string", "required": true },
                            "password": { "type": "string", "required": true },
                            "tokenUrl": { "type": "string", "required": true }
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

        // Configure UseStep exactly as shown in the problem statement
        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "name": "getToken",
            "type": "use",
            "template": "authenticate",
            "with": {
                "username": "{{$.env.username}}",
                "password": "{{$.env.password}}",
                "tokenUrl": "{{$.env.tokenUrl}}"
            },
            "save": {
                "$.globals.token": "{{$.this.token}}",
                "$.globals.authHeader": "{{$.this.authHeader}}"
            }
        }
        """);
        useStep.ValidateConfiguration(config);

        var context = new TestExecutionContext();
        context.Variables["env"] = new Dictionary<string, object>
        {
            ["username"] = "testuser",
            ["password"] = "secret123",
            ["tokenUrl"] = "https://api.example.com/token"
        };
        context.Variables["globals"] = new Dictionary<string, object>();

        // Act
        var result = await useStep.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success, $"UseStep execution should succeed. Error: {result.ErrorMessage}");

        // Verify template outputs are accessible via {{$.this.outputKey}} pattern
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("testuser-secret123-token", thisResult["token"]);
        Assert.Equal("Bearer testuser-secret123-token", thisResult["authHeader"]);

        // Verify save operations work exactly as specified in the problem statement
        Assert.Contains("globals", context.Variables.Keys);
        var globals = Assert.IsType<Dictionary<string, object>>(context.Variables["globals"]);
        Assert.Equal("testuser-secret123-token", globals["token"]);
        Assert.Equal("Bearer testuser-secret123-token", globals["authHeader"]);

        // Verify the saved values can be accessed via JSONPath
        Assert.Equal("testuser-secret123-token",
            JTest.Core.Utilities.VariableInterpolator.ResolveVariableTokens("{{$.globals.token}}", context));
        Assert.Equal("Bearer testuser-secret123-token",
            JTest.Core.Utilities.VariableInterpolator.ResolveVariableTokens("{{$.globals.authHeader}}", context));
    }

    [Fact]
    public void HttpStep_SaveFunctionality_WorksCorrectly()
    {
        // Arrange
        var httpStep = new HttpStep(new HttpClient());

        var config = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "http",
            "method": "GET",
            "url": "https://httpbin.org/json",
            "save": {
                "$.globals.responseStatus": "{{$.this.status}}",
                "simpleVar": "{{$.this.contentType}}"
            }
        }
        """);

        httpStep.ValidateConfiguration(config);

        var context = new TestExecutionContext();
        context.Variables["globals"] = new Dictionary<string, object>();

        // Act - We need to mock this since we don't want to make real HTTP calls in tests
        // So let's just test the save processing directly
        context.Variables["this"] = new
        {
            status = 200,
            contentType = "application/json"
        };

        // Use reflection to call the protected ProcessSaveOperations method
        var processMethod = typeof(BaseStep).GetMethod("ProcessSaveOperations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        processMethod?.Invoke(httpStep, new object[] { context });

        // Assert
        var globals = Assert.IsType<Dictionary<string, object>>(context.Variables["globals"]);
        Assert.Equal(200, globals["responseStatus"]);
        Assert.Equal("application/json", context.Variables["simpleVar"]);
    }
}