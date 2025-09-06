using System.Text.Json.Serialization;

namespace JTest.Core.Models;

/// <summary>
/// Represents a test suite containing multiple test cases with shared configuration
/// </summary>
public class JTestSuite
{
    /// <summary>
    /// Gets or sets the version of the test suite format
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata information for the test suite
    /// </summary>
    [JsonPropertyName("info")]
    public JTestInfo? Info { get; set; }

    /// <summary>
    /// Gets or sets the list of template files to include
    /// </summary>
    [JsonPropertyName("using")]
    public List<string>? Using { get; set; }

    /// <summary>
    /// Gets or sets the environment variables for the test suite
    /// </summary>
    [JsonPropertyName("env")]
    public Dictionary<string, object>? Env { get; set; }

    /// <summary>
    /// Gets or sets the global variables for the test suite
    /// </summary>
    [JsonPropertyName("globals")]
    public Dictionary<string, object>? Globals { get; set; }

    /// <summary>
    /// Gets or sets the list of test cases in the suite
    /// </summary>
    [JsonPropertyName("tests")]
    public List<JTestCase> Tests { get; set; } = new();
}