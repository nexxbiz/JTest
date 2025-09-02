using System.Text.Json;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;

namespace JTest.Core;

/// <summary>
/// Core functionality for JTest - a test suite for testing APIs using JSON definitions
/// </summary>
public class TestRunner
{
    private readonly TestCaseExecutor _executor;
    private readonly StepFactory _stepFactory;
    private readonly TemplateProvider _templateProvider;

    public TestRunner()
    {
        _templateProvider = new TemplateProvider();
        _stepFactory = new StepFactory(_templateProvider);
        _executor = new TestCaseExecutor(_stepFactory);
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
            
            // Check if this is a test suite
            if (root.TryGetProperty("version", out _) && 
                root.TryGetProperty("tests", out var testsElement) && 
                testsElement.ValueKind == JsonValueKind.Array)
            {
                // Test suite validation
                return ValidateTestSuite(root);
            }
            // For backwards compatibility, allow basic JSON validation
            // If it has 'name' and 'steps', validate as JTest schema
            else if (root.TryGetProperty("name", out _) || root.TryGetProperty("steps", out _))
            {
                // JTest schema validation
                if (!root.TryGetProperty("name", out _))
                    return false;
                    
                if (!root.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
                    return false;
            }
            
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool ValidateTestSuite(JsonElement root)
    {
        // Validate required fields
        if (!root.TryGetProperty("version", out _))
            return false;
            
        if (!root.TryGetProperty("tests", out var testsElement) || testsElement.ValueKind != JsonValueKind.Array)
            return false;
            
        // Validate each test case in the tests array
        foreach (var testElement in testsElement.EnumerateArray())
        {
            if (!testElement.TryGetProperty("name", out _))
                return false;
                
            if (!testElement.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Runs a test from JSON definition with optional debug logging
    /// </summary>
    /// <param name="jsonDefinition">The JSON test definition (single test case or test suite)</param>
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
        // Update executor to use debug logger if provided
        if (debugLogger != null)
        {
            _stepFactory.SetDebugLogger(debugLogger);
        }

        // Detect if this is a test suite or individual test case
        if (IsTestSuite(jsonDefinition))
        {
            return await RunTestSuiteAsync(jsonDefinition, environment, globals);
        }
        else
        {
            var testCase = ParseTestCase(jsonDefinition);
            var context = CreateExecutionContext(environment, globals);
            return await _executor.ExecuteAsync(testCase, context);
        }
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
    /// Loads templates from JSON definition
    /// </summary>
    /// <param name="jsonDefinition">The JSON template definition</param>
    public void LoadTemplates(string jsonDefinition)
    {
        _templateProvider.LoadTemplatesFromJson(jsonDefinition);
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

    private bool IsTestSuite(string jsonDefinition)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonDefinition);
            var root = jsonDoc.RootElement;
            
            // Check if it has the test suite structure (version and tests array)
            return root.TryGetProperty("version", out _) && 
                   root.TryGetProperty("tests", out var testsElement) && 
                   testsElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<List<JTestCaseResult>> RunTestSuiteAsync(
        string jsonDefinition, 
        Dictionary<string, object>? environment = null,
        Dictionary<string, object>? globals = null)
    {
        var testSuite = ParseTestSuite(jsonDefinition);
        
        // Merge environment variables (parameter takes precedence)
        var mergedEnvironment = MergeDictionaries(testSuite.Env, environment);
        
        // Merge global variables (parameter takes precedence)
        var mergedGlobals = MergeDictionaries(testSuite.Globals, globals);
        
        var allResults = new List<JTestCaseResult>();
        
        // Execute each test case in the suite
        foreach (var testCase in testSuite.Tests)
        {
            var context = CreateExecutionContext(mergedEnvironment, mergedGlobals);
            var results = await _executor.ExecuteAsync(testCase, context);
            allResults.AddRange(results);
        }
        
        return allResults;
    }

    private JTestSuite ParseTestSuite(string jsonDefinition)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var testSuite = JsonSerializer.Deserialize<JTestSuite>(jsonDefinition, options);
        if (testSuite == null)
            throw new ArgumentException("Invalid test suite JSON");
            
        return testSuite;
    }

    private Dictionary<string, object> MergeDictionaries(
        Dictionary<string, object>? source, 
        Dictionary<string, object>? target)
    {
        var result = new Dictionary<string, object>();
        
        // Add source first
        if (source != null)
        {
            foreach (var kvp in source)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        
        // Add target, overriding source values
        if (target != null)
        {
            foreach (var kvp in target)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        
        return result;
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
