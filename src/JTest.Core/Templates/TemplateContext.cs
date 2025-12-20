using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Utilities;
using System.Text.Json;

namespace JTest.Core.Templates;

public sealed class TemplateContext(HttpClient httpClient, GlobalConfigurationAccessor globalConfigurationAccessor, JsonSerializerOptionsCache jsonSerializerOptionsCache) : ITemplateContext
{
    private readonly Dictionary<string, Template> _templates = [];

    public async Task Load(JTestSuite testSuite, IExecutionContext executionContext)
    {
        _templates.Clear();

        // Load templates from GlobalConfiguration
        await LoadTemplatesFromGlobalUsingPathsAsync(executionContext);

        // Load templates from using statement before any test execution        
        await LoadTemplatesFromUsingAsync(testSuite.Using, executionContext, testSuite.FilePath);
    }

    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="name">The template name</param>
    /// <returns>The template definition or null if not found</returns>
    public Template GetTemplate(string name)
    {
        return _templates.TryGetValue(name, out var template)
            ? template
            : throw new InvalidOperationException($"Template '{name}' not found");
    }

    /// <summary>
    /// Registers a template collection
    /// </summary>
    /// <param name="templateCollection">The template collection to register</param>
    private void RegisterTemplateCollection(TemplateCollection templateCollection)
    {
        if (templateCollection.Components?.Templates == null) return;

        foreach (var template in templateCollection.Components.Templates)
        {
            _templates[template.Name] = template;
        }
    }

    /// <summary>
    /// Loads templates from JSON content
    /// </summary>
    /// <param name="jsonContent">The JSON content containing templates</param>
    private void LoadTemplatesFromJson(string jsonContent)
    {
        var templateCollection = JsonSerializer.Deserialize<TemplateCollection>(jsonContent, jsonSerializerOptionsCache.Options);
        if (templateCollection != null)
        {
            RegisterTemplateCollection(templateCollection);
        }
    }

    /// <summary>
    /// Loads templates from the using statement before test execution
    /// </summary>
    /// <param name="usingPaths">List of template file paths or URLs</param>
    /// <param name="context">Execution context for logging</param>
    /// <param name="testFilePath">Optional path to test file for resolving relative template paths</param>
    private async Task LoadTemplatesFromUsingAsync(List<string>? usingPaths, IExecutionContext context, string? testFilePath = null)
    {
        if (usingPaths == null || usingPaths.Count == 0)
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

                LoadTemplatesFromJson(templateContent);
                context.Log.Add($"Successfully loaded templates from: {path}");
            }
            catch (Exception ex)
            {
                context.Log.Add($"Error loading templates from '{path}': {ex.Message}");
                throw new InvalidOperationException($"Failed to load templates from '{path}': {ex.Message}", ex);
            }
        }
    }

    private Task LoadTemplatesFromGlobalUsingPathsAsync(IExecutionContext context)
    {
        var globalConfiguration = globalConfigurationAccessor.Get();
        if (globalConfiguration.Templates is null)
            return Task.CompletedTask;

        var usingPaths = new List<string>();

        usingPaths.AddRange(
            FindTemplateJsonFilesInSearchPaths(globalConfiguration.Templates.SearchPaths)
        );

        var resolvedTemplatePaths = globalConfiguration.Templates.Paths?.Select(path =>
        {
            return ResolveTemplatePath(path, testFilePath: null);
        });
        usingPaths.AddRange(
            resolvedTemplatePaths ?? []
        );

        return LoadTemplatesFromUsingAsync(usingPaths, context);
    }

    private static IEnumerable<string> FindTemplateJsonFilesInSearchPaths(IEnumerable<string>? searchPaths)
    {
        if (searchPaths is null)
        {
            return [];
        }

        return searchPaths.SelectMany(searchPath =>
        {
            if (string.IsNullOrWhiteSpace(searchPath) || !Directory.Exists(searchPath))
            {
                return [];
            }

            var jsonFiles = JsonFileSearcher.Search(searchPaths);

            return jsonFiles;
        });
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
    /// Loads content from either a file path or HTTP URL
    /// </summary>
    /// <param name="path">File path or HTTP URL</param>
    /// <returns>The content as string</returns>
    private async Task<string> LoadContentFromPathAsync(string path)
    {
        if (IsHttpUrl(path))
        {
            var response = await httpClient.GetAsync(path);
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
