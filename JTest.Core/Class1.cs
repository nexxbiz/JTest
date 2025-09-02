using System.Text.Json;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.Core;

/// <summary>
/// Core functionality for JTest - a test suite for testing APIs using JSON definitions
/// </summary>
public class TestRunner
{
    private readonly TestCaseExecutor _executor;
    private readonly StepFactory _stepFactory;

    public TestRunner()
    {
        _executor = new TestCaseExecutor();
        _stepFactory = new StepFactory();
    }

    /// <summary>
    /// Gets the current version of JTest
    /// </summary>
    public string Version => "1.0.0";
    
    /// <summary>
    /// Validates if a JSON test definition is well-formed
    /// </summary>
    /// <param name="jsonDefinition">The JSON test definition to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateTestDefinition(string jsonDefinition)
    {
        if (string.IsNullOrWhiteSpace(jsonDefinition))
            return false;
            
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonDefinition);
            var root = jsonDoc.RootElement;
            
            // For backwards compatibility, allow basic JSON validation
            // If it has 'name' and 'flow', validate as JTest schema
            if (root.TryGetProperty("name", out _) || root.TryGetProperty("flow", out _))
            {
                // JTest schema validation
                if (!root.TryGetProperty("name", out _))
                    return false;
                    
                if (!root.TryGetProperty("flow", out var flowElement) || flowElement.ValueKind != JsonValueKind.Array)
                    return false;
            }
            
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Runs a test from JSON definition with optional debug logging
    /// </summary>
    /// <param name="jsonDefinition">The JSON test definition</param>
    /// <param name="environment">Environment variables</param>
    /// <param name="globals">Global variables</param>
    /// <param name="debugLogger">Optional debug logger for detailed output</param>
    /// <returns>Test execution results</returns>
    public async Task<List<JTestCaseResult>> RunTestAsync(
        string jsonDefinition, 
        Dictionary<string, object>? environment = null,
        Dictionary<string, object>? globals = null,
        IDebugLogger? debugLogger = null)
    {
        var testCase = ParseTestCase(jsonDefinition);
        var context = CreateExecutionContext(environment, globals);
        
        // Update executor to use debug logger if provided
        if (debugLogger != null)
        {
            _stepFactory.SetDebugLogger(debugLogger);
        }
        
        return await _executor.ExecuteAsync(testCase, context);
    }
    
    /// <summary>
    /// Creates a basic test template
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <returns>JSON test template</returns>
    public string CreateTestTemplate(string testName)
    {
        var template = new
        {
            name = testName,
            flow = new object[]
            {
                new
                {
                    type = "http",
                    method = "GET",
                    url = "{{$.env.baseUrl}}/api/endpoint",
                    save = new { response = "{{$.this.body}}" },
                    assert = new object[]
                    {
                        new { equals = new { actual = "{{$.this.status}}", expected = 200 } }
                    }
                }
            }
        };
        
        return JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
    }
    
    /// <summary>
    /// Gets a welcome message for the JTest tool
    /// </summary>
    /// <returns>Welcome message string</returns>
    public string GetWelcomeMessage()
    {
        return $"Welcome to JTest v{Version} - API Testing Tool";
    }
    
    private JTestCase ParseTestCase(string jsonDefinition)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var testCase = JsonSerializer.Deserialize<JTestCase>(jsonDefinition, options);
        if (testCase == null)
            throw new ArgumentException("Invalid test case JSON");
            
        return testCase;
    }
    
    private TestExecutionContext CreateExecutionContext(
        Dictionary<string, object>? environment = null,
        Dictionary<string, object>? globals = null)
    {
        var context = new TestExecutionContext();
        
        if (environment != null)
        {
            context.Variables["env"] = environment;
        }
        
        if (globals != null)
        {
            context.Variables["globals"] = globals;
        }
        else
        {
            context.Variables["globals"] = new Dictionary<string, object>();
        }
        
        context.Variables["ctx"] = new Dictionary<string, object>();
        
        return context;
    }
}
