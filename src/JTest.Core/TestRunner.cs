using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Templates;
using System.Text.Json;

namespace JTest.Core;

/// <summary>
/// Core functionality for JTest - a test suite for testing APIs using JSON definitions
/// </summary>
public class TestRunner
{
    private readonly TestCaseExecutor _executor;
    private readonly StepFactory _stepFactory;
    private readonly TemplateProvider _templateProvider;
    private readonly HttpClient _httpClient;

    public TestRunner()
    {
        _templateProvider = new TemplateProvider();
        _stepFactory = new StepFactory(_templateProvider);
        _executor = new TestCaseExecutor(_stepFactory);
        _httpClient = new HttpClient();
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
        Dictionary<string, object>? globals = null)
    {
        return await RunTestAsync(jsonDefinition, null, environment, globals);
    }

    /// <summary>
    /// Runs a test from JSON definition with test file path context for resolving relative template paths
    /// </summary>
    /// <param name="jsonDefinition">The JSON test definition (single test case or test suite)</param>
    /// <param name="testFilePath">Optional path to the test file for resolving relative template paths</param>
    /// <param name="environment">Environment variables</param>
    /// <param name="globals">Global variables</param>
    /// <returns>Test execution results</returns>
    public async Task<List<JTestCaseResult>> RunTestAsync(
        string jsonDefinition,
        string? testFilePath,
        Dictionary<string, object>? environment = null,
        Dictionary<string, object>? globals = null)
    {
        // Detect if this is a test suite or individual test case
        if (IsTestSuite(jsonDefinition))
        {
            return await RunTestSuiteAsync(jsonDefinition, testFilePath, environment, globals);
        }
        else
        {
            var testCase = ParseTestCase(jsonDefinition);
            var context = CreateExecutionContext(environment, globals);
            return await _executor.ExecuteAsync(testCase, context, 1);
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
        return await RunTestSuiteAsync(jsonDefinition, null, environment, globals);
    }

    private async Task<List<JTestCaseResult>> RunTestSuiteAsync(
        string jsonDefinition,
        string? testFilePath,
        Dictionary<string, object>? environment = null,
        Dictionary<string, object>? globals = null)
    {
        var testSuite = ParseTestSuite(jsonDefinition);

        // Merge environment variables (parameter takes precedence)
        var mergedEnvironment = MergeDictionaries(testSuite.Env, environment);

        // Merge global variables (parameter takes precedence)
        var mergedGlobals = MergeDictionaries(testSuite.Globals, globals);

        // Create temporary context for template loading logging
        var tempContext = CreateExecutionContext(mergedEnvironment, mergedGlobals);

        // Load templates from using statement before any test execution
        await LoadTemplatesFromUsingAsync(testSuite.Using, tempContext, testFilePath);

        var allResults = new List<JTestCaseResult>();

        // Execute each test case in the suite
        int testNumber = 1;
        foreach (var testCase in testSuite.Tests)
        {
            var context = CreateExecutionContext(mergedEnvironment, mergedGlobals);
            var results = await _executor.ExecuteAsync(testCase, context, testNumber);
            allResults.AddRange(results);
            testNumber++;
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

    /// <summary>
    /// Loads templates from the using statement before test execution
    /// </summary>
    /// <param name="usingPaths">List of template file paths or URLs</param>
    /// <param name="context">Execution context for logging</param>
    /// <param name="testFilePath">Optional path to test file for resolving relative template paths</param>
    private async Task LoadTemplatesFromUsingAsync(List<string>? usingPaths, IExecutionContext context, string? testFilePath = null)
    {
        if (usingPaths == null || !usingPaths.Any())
            return;

        var loadedTemplateNames = new HashSet<string>();

        foreach (var path in usingPaths)
        {
            try
            {
                // Resolve relative paths relative to the test file directory, not the current working directory
                var resolvedPath = ResolveTemplatePath(path, testFilePath);
                context.Log.Add($"Loading templates from: {resolvedPath}");

                string templateContent = await LoadContentFromPathAsync(resolvedPath);

                // Parse to check for template names before loading
                var templateNames = GetTemplateNamesFromJson(templateContent);

                // Check for overwrites and log warnings
                foreach (var templateName in templateNames)
                {
                    if (loadedTemplateNames.Contains(templateName))
                    {
                        context.Log.Add($"Warning: Template '{templateName}' from '{path}' overwrites previously loaded template");
                    }
                    loadedTemplateNames.Add(templateName);
                }

                _templateProvider.LoadTemplatesFromJson(templateContent);
                context.Log.Add($"Successfully loaded templates from: {path}");
            }
            catch (Exception ex)
            {
                context.Log.Add($"Error loading templates from '{path}': {ex.Message}");
                throw new InvalidOperationException($"Failed to load templates from '{path}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Loads content from either a file path or HTTP URL
    /// </summary>
    /// <param name="path">File path or HTTP URL</param>
    /// <returns>The content as string</returns>
    private async Task<string> LoadContentFromPathAsync(string path)
    {
        if (IsHttpUrl(path))
        {
            var response = await _httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            return await File.ReadAllTextAsync(path);
        }
    }

    /// <summary>
    /// Checks if a path is an HTTP URL
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if HTTP URL, false otherwise</returns>
    private static bool IsHttpUrl(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves template paths relative to the test file directory when available,
    /// otherwise uses the current working directory
    /// </summary>
    /// <param name="templatePath">The template path from the 'using' statement</param>
    /// <param name="testFilePath">Optional path to the test file</param>
    /// <returns>The resolved template path</returns>
    private static string ResolveTemplatePath(string templatePath, string? testFilePath)
    {
        // If it's an HTTP URL or an absolute path, return as-is
        if (IsHttpUrl(templatePath) || Path.IsPathRooted(templatePath))
        {
            return templatePath;
        }

        // If we have a test file path and the template path is relative,
        // resolve it relative to the test file directory
        if (!string.IsNullOrEmpty(testFilePath))
        {
            var testFileDirectory = Path.GetDirectoryName(testFilePath);
            if (!string.IsNullOrEmpty(testFileDirectory))
            {
                return Path.GetFullPath(Path.Combine(testFileDirectory, templatePath));
            }
        }

        // Fallback to resolving relative to current working directory
        return Path.GetFullPath(templatePath);
    }

    /// <summary>
    /// Extracts template names from JSON content without fully deserializing
    /// </summary>
    /// <param name="jsonContent">The JSON content</param>
    /// <returns>List of template names</returns>
    private static List<string> GetTemplateNamesFromJson(string jsonContent)
    {
        var names = new List<string>();

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("components", out var components) &&
                components.TryGetProperty("templates", out var templates) &&
                templates.ValueKind == JsonValueKind.Array)
            {
                foreach (var template in templates.EnumerateArray())
                {
                    if (template.TryGetProperty("name", out var nameElement) &&
                        nameElement.ValueKind == JsonValueKind.String)
                    {
                        names.Add(nameElement.GetString() ?? "");
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return empty list - the main loading will handle the error
        }

        return names;
    }
}
