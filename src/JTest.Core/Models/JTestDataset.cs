using System.Text.Json.Serialization;

namespace JTest.Core.Models;

/// <summary>
/// Represents a dataset for data-driven testing with case variables
/// </summary>
public class JTestDataset
{
    /// <summary>
    /// Gets or sets the name of the dataset
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the case variables for this dataset iteration
    /// </summary>
    [JsonPropertyName("case")]
    public Dictionary<string, object> Case { get; set; } = [];
}