using System.Text.Json;
using JTest.Core.Debugging;

namespace JTest.Core.Steps;

/// <summary>
/// Factory for creating step instances from JSON configuration
/// </summary>
public class StepFactory
{
    private readonly HttpClient _httpClient;
    private IDebugLogger? _debugLogger;
    
    public StepFactory()
    {
        _httpClient = new HttpClient();
    }
    
    /// <summary>
    /// Sets the debug logger for created steps
    /// </summary>
    public void SetDebugLogger(IDebugLogger? debugLogger)
    {
        _debugLogger = debugLogger;
    }
    
    /// <summary>
    /// Creates a step instance from JSON configuration
    /// </summary>
    /// <param name="stepConfig">The JSON configuration object</param>
    /// <returns>The created step instance</returns>
    /// <exception cref="ArgumentException">Thrown when step type is unknown or configuration is invalid</exception>
    public IStep CreateStep(object stepConfig)
    {
        var json = JsonSerializer.Serialize(stepConfig);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (!jsonElement.TryGetProperty("type", out var typeElement))
        {
            throw new ArgumentException("Step configuration must have a 'type' property");
        }
        
        var stepType = typeElement.GetString();
        
        IStep step = stepType?.ToLowerInvariant() switch
        {
            "http" => new HttpStep(_httpClient, _debugLogger),
            "wait" => new WaitStep(),
            _ => throw new ArgumentException($"Unknown step type: {stepType}")
        };
        
        // Set step ID if provided
        if (jsonElement.TryGetProperty("id", out var idElement))
        {
            step.Id = idElement.GetString();
        }
        
        // Validate configuration
        if (!step.ValidateConfiguration(jsonElement))
        {
            throw new ArgumentException($"Invalid configuration for step type '{stepType}'");
        }
        
        return step;
    }
    
    /// <summary>
    /// Disposes resources used by the factory
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}