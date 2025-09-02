using JTest.Core.Models;

namespace JTest.Core.Steps;

/// <summary>
/// Interface for providing template definitions
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="name">The template name</param>
    /// <returns>The template definition or null if not found</returns>
    Template? GetTemplate(string name);

    /// <summary>
    /// Registers a template collection
    /// </summary>
    /// <param name="templateCollection">The template collection to register</param>
    void RegisterTemplateCollection(TemplateCollection templateCollection);

    /// <summary>
    /// Loads templates from JSON content
    /// </summary>
    /// <param name="jsonContent">The JSON content containing templates</param>
    void LoadTemplatesFromJson(string jsonContent);
}