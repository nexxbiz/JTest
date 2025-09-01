namespace JTest.Core;

/// <summary>
/// Core functionality for JTest - a test suite for testing APIs using JSON definitions
/// </summary>
public class TestRunner
{
    /// <summary>
    /// Gets the current version of JTest
    /// </summary>
    public string Version => "1.0.0";
    
    /// <summary>
    /// Validates if a JSON test definition is well-formed
    /// </summary>
    /// <param name="jsonDefinition">The JSON test definition to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateTestDefinition(string jsonDefinition)
    {
        if (string.IsNullOrWhiteSpace(jsonDefinition))
            return false;
            
        // Basic validation - just check if it's not empty for now
        return jsonDefinition.Trim().StartsWith("{") && jsonDefinition.Trim().EndsWith("}");
    }
    
    /// <summary>
    /// Gets a welcome message for the JTest tool
    /// </summary>
    /// <returns>Welcome message string</returns>
    public string GetWelcomeMessage()
    {
        return $"Welcome to JTest v{Version} - API Testing Tool";
    }
}
