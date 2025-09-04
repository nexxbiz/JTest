using JTest.Core.Utilities;

namespace JTest.Core.Execution;

/// <summary>
/// Default implementation of IExecutionContext for test execution.
/// 
/// During dataset iterations, proper cleanup is implemented via TestCaseExecutor:
/// - env variables: Immutable, preserved across iterations
/// - globals: Shared state, modifications persist across iterations
/// - other variables (ctx, this, named): Reset to original values for each iteration
/// </summary>
public class TestExecutionContext : IExecutionContext
{
    /// <summary>
    /// Gets the variables dictionary containing all execution variables
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new();

    /// <summary>
    /// Gets the log list for warnings and errors during execution
    /// </summary>
    public IList<string> Log { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the current test number
    /// </summary>
    public int TestNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current step number within the test
    /// </summary>
    public int StepNumber { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the current test case name
    /// </summary>
    public string TestCaseName { get; set; } = string.Empty;

    /// <summary>
    /// Sets the case context variables for the current dataset iteration
    /// Automatically resolves any tokens in the case data that reference other variables
    /// </summary>
    /// <param name="caseData">The case variables to set in the context</param>
    public void SetCase(Dictionary<string, object> caseData)
    {
        // Resolve any tokens in the case data before setting it
        var resolvedCaseData = ResolveCaseTokens(caseData);
        Variables["case"] = resolvedCaseData;
    }

    /// <summary>
    /// Clears the case context (sets to empty dictionary)
    /// </summary>
    public void ClearCase()
    {
        Variables["case"] = new Dictionary<string, object>();
    }

    /// <summary>
    /// Recursively resolves any tokens in case data before setting it in the context
    /// This ensures that tokens referencing env, globals, or other variables are resolved
    /// when the case is set, rather than when individual properties are accessed
    /// </summary>
    /// <param name="caseData">The case data that may contain tokens</param>
    /// <returns>Case data with all tokens resolved</returns>
    private Dictionary<string, object> ResolveCaseTokens(Dictionary<string, object> caseData)
    {
        var resolvedData = new Dictionary<string, object>();
        
        foreach (var kvp in caseData)
        {
            resolvedData[kvp.Key] = ResolveValue(kvp.Value);
        }
        
        return resolvedData;
    }

    /// <summary>
    /// Recursively resolves tokens in a value (string, object, or dictionary)
    /// </summary>
    /// <param name="value">The value that may contain tokens</param>
    /// <returns>The value with tokens resolved</returns>
    private object ResolveValue(object value)
    {
        switch (value)
        {
            case string stringValue:
                // Use VariableInterpolator to resolve any tokens in string values
                return VariableInterpolator.ResolveVariableTokens(stringValue, this);
                
            case Dictionary<string, object> dictValue:
                // Recursively resolve tokens in nested dictionaries
                var resolvedDict = new Dictionary<string, object>();
                foreach (var kvp in dictValue)
                {
                    resolvedDict[kvp.Key] = ResolveValue(kvp.Value);
                }
                return resolvedDict;
                
            case System.Collections.IEnumerable enumerable when !(value is string):
                // Handle arrays and lists by resolving each element
                var resolvedList = new List<object>();
                foreach (var item in enumerable)
                {
                    resolvedList.Add(ResolveValue(item));
                }
                return resolvedList.ToArray(); // Convert back to array to match input type
                
            default:
                // For primitive types and complex objects, check if they contain string properties that might have tokens
                return ResolveComplexObject(value) ?? value;
        }
    }

    /// <summary>
    /// Handles complex objects by serializing and checking for tokens
    /// </summary>
    /// <param name="value">The complex object to check for tokens</param>
    /// <returns>The object with any string properties containing tokens resolved</returns>
    private object ResolveComplexObject(object value)
    {
        if (value == null) return value;
        
        // For simple value types, return as-is
        var type = value.GetType();
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal))
        {
            return value;
        }
        
        try
        {
            // Serialize to JSON to check for string properties that might contain tokens
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            
            // If the JSON representation contains tokens, we need to process it
            if (json.Contains("{{$."))
            {
                // Parse as JsonElement and recursively resolve
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                return ResolveJsonElement(jsonElement);
            }
            
            // No tokens found, return original
            return value;
        }
        catch
        {
            // If serialization fails, return original value
            return value;
        }
    }

    /// <summary>
    /// Resolves tokens in a JsonElement structure
    /// </summary>
    /// <param name="element">The JsonElement to process</param>
    /// <returns>The resolved value</returns>
    private object ResolveJsonElement(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                var stringValue = element.GetString() ?? "";
                return VariableInterpolator.ResolveVariableTokens(stringValue, this);
                
            case System.Text.Json.JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ResolveJsonElement(property.Value);
                }
                return dict;
                
            case System.Text.Json.JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ResolveJsonElement(item));
                }
                return list.ToArray();
                
            case System.Text.Json.JsonValueKind.Number:
                return element.TryGetInt32(out var intValue) ? intValue : element.GetDouble();
                
            case System.Text.Json.JsonValueKind.True:
                return true;
                
            case System.Text.Json.JsonValueKind.False:
                return false;
                
            case System.Text.Json.JsonValueKind.Null:
                return (object?)null;
                
            default:
                return element;
        }
    }
}