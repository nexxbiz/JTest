using JTest.Core.Models;

namespace JTest.Core.Templates;

/// <summary>
/// Interface for providing template definitions
/// </summary>
public interface ITemplateContext
{
    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="name">The template name</param>
    /// <returns>The template definition or null if not found</returns>
    Template GetTemplate(string name);

    Task Load(JTestSuite testSuite);
}