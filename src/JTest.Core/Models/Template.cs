using JTest.Core.Steps;
using System.Text.Json.Serialization;

namespace JTest.Core.Models;

/// <summary>
/// Represents a test template with parameters, steps, and output mapping
/// </summary>
public class Template
{
    /// <summary>
    /// Gets or sets the template name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template parameters
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, TemplateParameter>? Params { get; set; }

    /// <summary>
    /// Gets or sets the steps to execute within the template
    /// </summary>
    [JsonPropertyName("steps")]
    public IEnumerable<IStep> Steps { get; set; } = [];

    /// <summary>
    /// Gets or sets the output mapping that defines what values are exposed to the parent context
    /// </summary>
    [JsonPropertyName("output")]
    public Dictionary<string, object?>? Output { get; set; }
}

/// <summary>
/// Represents a template parameter definition
/// </summary>
public class TemplateParameter
{
    /// <summary>
    /// Gets or sets the parameter type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// Gets or sets whether the parameter is required
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets the parameter description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default value for the parameter
    /// </summary>
    [JsonPropertyName("default")]
    public object? Default { get; set; }
}

/// <summary>
/// Represents a collection of templates
/// </summary>
public class TemplateCollection
{
    /// <summary>
    /// Gets or sets the version of the template collection
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the components containing templates
    /// </summary>
    [JsonPropertyName("components")]
    public TemplateComponents? Components { get; set; }
}

/// <summary>
/// Represents the components section of a template collection
/// </summary>
public class TemplateComponents
{
    /// <summary>
    /// Gets or sets the list of templates
    /// </summary>
    [JsonPropertyName("templates")]
    public List<Template> Templates { get; set; } = [];
}