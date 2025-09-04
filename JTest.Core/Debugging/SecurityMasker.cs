using System.Text.RegularExpressions;

namespace JTest.Core.Debugging;

/// <summary>
/// Handles security-sensitive value masking for debug output
/// </summary>
public class SecurityMasker
{
    private readonly List<(string original, string masked)> _maskingPairs = new();
    private readonly string[] _securityKeys = { "password", "token", "secret", "key", "credential", "auth", "authorization", "bearer" };

    /// <summary>
    /// Registers a value for masking if it contains security-sensitive data
    /// </summary>
    /// <param name="key">The key name to check for security sensitivity</param>
    /// <param name="value">The value to potentially mask</param>
    /// <returns>The masked value if security-sensitive, otherwise the original value</returns>
    public object RegisterForMasking(string key, object value)
    {
        if (value == null) return "null";
        
        var isSecuritySensitive = _securityKeys.Any(sk => key.ToLowerInvariant().Contains(sk));
        
        if (isSecuritySensitive)
        {
            var originalValue = value.ToString() ?? "";
            
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                // For strings, add both quoted and unquoted versions for masking
                _maskingPairs.Add((str, "***masked***"));
                _maskingPairs.Add(($"\"{str}\"", "\"***masked***\""));
                return "\"***masked***\"";
            }
            else
            {
                _maskingPairs.Add((originalValue, "***masked***"));
                return "***masked***";
            }
        }
        
        return value;
    }

    /// <summary>
    /// Applies all collected masking rules to the final text output
    /// </summary>
    /// <param name="text">The text to apply masking to</param>
    /// <returns>The text with all sensitive values masked</returns>
    public string ApplyMasking(string text)
    {
        var result = text;
        
        // Sort by length descending to avoid partial replacements
        var sortedPairs = _maskingPairs
            .Where(p => !string.IsNullOrEmpty(p.original) && p.original != p.masked)
            .OrderByDescending(p => p.original.Length)
            .ToList();
        
        foreach (var (original, masked) in sortedPairs)
        {
            // Use simple string replacement instead of regex for reliability
            result = result.Replace(original, masked);
        }
        
        return result;
    }

    /// <summary>
    /// Clears all registered masking pairs
    /// </summary>
    public void Clear()
    {
        _maskingPairs.Clear();
    }
}