using JTest.Core.Models;
using JTest.Core.Templates;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Factory for creating step instances from JSON configuration
/// </summary>
public class StepFactory
{
    private readonly HttpClient _httpClient;
    private readonly ITemplateContext _templateProvider;

    public StepFactory(ITemplateContext? templateProvider = null)
    {
        _httpClient = new HttpClient();
        _templateProvider = templateProvider ?? new Templates.TemplateCollection();
    }

    /// <summary>
    /// Creates a step instance from JSON configuration
    /// </summary>
    /// <param name="stepConfig">The JSON configuration object</param>
    /// <returns>The created step instance</returns>
    /// <exception cref="ArgumentException">Thrown when step type is unknown or configuration is invalid</exception>
    public virtual IStep CreateStep(object stepConfig)
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
            "http" => new HttpStep(_httpClient, jsonElement),
            "wait" => new WaitStep(jsonElement),
            "use" => new UseStep(_templateProvider, this, jsonElement),
            "assert" => new AssertStep(jsonElement),
            _ => throw new ArgumentException($"Unknown step type: {stepType}")
        };

        var validationErrors = new List<string>();

        // Validate configuration
        if (!step.ValidateConfiguration(validationErrors))
        {
            throw new StepConfigurationValidationException(stepType, validationErrors);
        }

        return step;
    }

    /// <summary>
    /// Gets the template provider
    /// </summary>
    public ITemplateContext TemplateProvider => _templateProvider;

    /// <summary>
    /// Disposes resources used by the factory
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}