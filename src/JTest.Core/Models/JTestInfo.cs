using System.Text.Json.Serialization;

namespace JTest.Core.Models;

/// <summary>
/// Represents metadata information for a test suite
/// </summary>
public class JTestInfo
{
    /// <summary>
    /// Gets or sets the name of the test suite
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the test suite
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}