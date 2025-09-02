using System.Text.Json;
using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.Core.Templates;

/// <summary>
/// Default implementation of ITemplateProvider that manages template collections
/// </summary>
public class TemplateProvider : ITemplateProvider
{
    private readonly Dictionary<string, Template> _templates = new();

    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="name">The template name</param>
    /// <returns>The template definition or null if not found</returns>
    public Template? GetTemplate(string name)
    {
        return _templates.TryGetValue(name, out var template) ? template : null;
    }

    /// <summary>
    /// Registers a template collection
    /// </summary>
    /// <param name="templateCollection">The template collection to register</param>
    public void RegisterTemplateCollection(TemplateCollection templateCollection)
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
    public void LoadTemplatesFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var templateCollection = JsonSerializer.Deserialize<TemplateCollection>(jsonContent, options);
        if (templateCollection != null)
        {
            RegisterTemplateCollection(templateCollection);
        }
    }

    /// <summary>
    /// Gets all registered template names
    /// </summary>
    /// <returns>Collection of template names</returns>
    public IEnumerable<string> GetTemplateNames()
    {
        return _templates.Keys;
    }

    /// <summary>
    /// Clears all registered templates
    /// </summary>
    public void Clear()
    {
        _templates.Clear();
    }

    /// <summary>
    /// Gets the count of registered templates
    /// </summary>
    public int Count => _templates.Count;
}