using JTest.Core.Models;
using JTest.Core.Utilities;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace JTest.Core.Templates;

public sealed class TemplateContext(
    IAnsiConsole console,
    HttpClient httpClient,
    IGlobalConfigurationAccessor globalConfigurationAccessor,
    JsonSerializerOptionsCache jsonSerializerOptionsCache
)
: ITemplateContext
{
    private readonly ConcurrentDictionary<string, Template> _templates = new();

    // Global templates: load once, immutable after publish
    private ReadOnlyDictionary<string, Template>? _globalTemplates;
    private Task? _globalInitTask;
    private readonly SemaphoreSlim _globalInitSemaphore = new(1, 1);

    public async Task Load(JTestSuite testSuite)
    {
        // Ensure global templates are loaded once and awaited by all callers
        await EnsureGlobalTemplatesLoadedAsync();

        // Load per-test-suite templates (overwrites allowed)
        await LoadTemplatesFromUsingAsync(
            _templates,
            testSuite.Using,
            testSuite.FilePath
        );
    }

    public Template GetTemplate(string name)
    {
        // JTest Suite template takes precedence
        if (_templates.TryGetValue(name, out var template))
            return template;

        if (_globalTemplates?.TryGetValue(name, out template) == true)
            return template;

        throw new InvalidOperationException($"Template '{name}' not found");
    }


    private async Task EnsureGlobalTemplatesLoadedAsync()
    {
        if (_globalInitTask != null)
        {
            await _globalInitTask;
            return;
        }

        await _globalInitSemaphore.WaitAsync();
        try
        {
            _globalInitTask ??= LoadTemplatesFromGlobalUsingPathsAsync();
        }
        finally
        {
            _globalInitSemaphore.Release();
        }

        await _globalInitTask;
    }

    private async Task LoadTemplatesFromGlobalUsingPathsAsync()
    {
        var dictionary = new Dictionary<string, Template>();

        var globalConfiguration = globalConfigurationAccessor.Get();
        if (globalConfiguration.Templates is not null)
        {
            var globalSearchPaths = globalConfiguration.Templates.SearchPaths ?? [];
            var usingPaths = JsonFileSearcher
                .Search(globalSearchPaths ?? [])
                .ToList();

            if (globalConfiguration.Templates.Paths is not null)
            {
                usingPaths.AddRange(
                    globalConfiguration.Templates.Paths.Select(p => ResolveTemplatePath(p, testFilePath: null))
                );
            }

            await LoadTemplatesFromUsingAsync(dictionary, usingPaths);
        }

        // Safe publication — immutable after this assignment
        _globalTemplates = dictionary.AsReadOnly();
    }

    private async Task LoadTemplatesFromUsingAsync(
        IDictionary<string, Template> dictionary,
        List<string>? usingPaths,
        string? testFilePath = null)
    {
        if (usingPaths == null || usingPaths.Count == 0)
            return;

        foreach (var path in usingPaths)
        {
            try
            {
                var resolvedPath = ResolveTemplatePath(path, testFilePath);
                console.WriteLine($"Loading templates from: {resolvedPath}");

                var templateContent = await LoadContentFromPathAsync(resolvedPath);
                var templateCollection = JsonSerializer.Deserialize<TemplateCollection>(templateContent, jsonSerializerOptionsCache.Options);

                foreach (var template in templateCollection?.Components?.Templates ?? [])
                {
                    if (dictionary.ContainsKey(template.Name))
                    {
                        console.WriteLine(
                            $"Warning: Template '{template.Name}' from '{path}' overwrites a previously loaded template",
                            new Style(foreground: Color.Yellow)
                        );
                    }

                    dictionary[template.Name] = template;
                }

                console.WriteLine($"Successfully loaded templates from: {path}");
            }
            catch (Exception ex)
            {
                console.WriteLine(
                    $"Error loading templates from '{path}': {ex.Message}",
                    new Style(foreground: Color.Red)
                );
                throw new InvalidOperationException(
                    $"Failed to load templates from '{path}'", ex);
            }
        }
    }


    private static string ResolveTemplatePath(string templatePath, string? testFilePath)
    {
        if (IsHttpUrl(templatePath) || Path.IsPathRooted(templatePath))
            return templatePath;

        if (!string.IsNullOrEmpty(testFilePath))
        {
            var dir = Path.GetDirectoryName(testFilePath);
            if (!string.IsNullOrEmpty(dir))
                return Path.GetFullPath(Path.Combine(dir, templatePath));
        }

        return Path.GetFullPath(templatePath);
    }

    private async Task<string> LoadContentFromPathAsync(string path)
    {
        if (IsHttpUrl(path))
        {
            var response = await httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        return await File.ReadAllTextAsync(path);
    }

    private static bool IsHttpUrl(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
