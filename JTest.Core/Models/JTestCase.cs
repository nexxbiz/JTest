using System.Text.Json.Serialization;

namespace JTest.Core.Models;

/// <summary>
/// Represents a test case with its flow and optional datasets for data-driven testing
/// </summary>
public class JTestCase
{
    /// <summary>
    /// Gets or sets the name of the test case
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test steps (sequence of steps to execute)
    /// </summary>
    [JsonPropertyName("steps")]
    public List<object> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the datasets for data-driven testing
    /// </summary>
    [JsonPropertyName("datasets")]
    public List<JTestDataset>? Datasets { get; set; }
}