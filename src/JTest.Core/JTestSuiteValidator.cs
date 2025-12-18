using System.Text.Json;

namespace JTest.Core
{
    internal class JTestSuiteValidator
    {
        /// <summary>
        /// Validates if a JSON test definition is well-formed
        /// </summary>
        /// <param name="jsonDefinition">The JSON test definition to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateTestDefinition(string jsonDefinition)
        {
            if (string.IsNullOrWhiteSpace(jsonDefinition))
                return false;

            try
            {
                var jsonDoc = JsonDocument.Parse(jsonDefinition);
                var root = jsonDoc.RootElement;

                // Check if this is a test suite
                if (root.TryGetProperty("version", out _) &&
                    root.TryGetProperty("tests", out var testsElement) &&
                    testsElement.ValueKind == JsonValueKind.Array)
                {
                    // Test suite validation
                    return ValidateTestSuite(root);
                }
                // For backwards compatibility, allow basic JSON validation
                // If it has 'name' and 'steps', validate as JTest schema
                else if (root.TryGetProperty("name", out _) || root.TryGetProperty("steps", out _))
                {
                    // JTest schema validation
                    if (!root.TryGetProperty("name", out _))
                        return false;

                    if (!root.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
                        return false;
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool ValidateTestSuite(JsonElement root)
        {
            // Validate required fields
            if (!root.TryGetProperty("version", out _))
                return false;

            if (!root.TryGetProperty("tests", out var testsElement) || testsElement.ValueKind != JsonValueKind.Array)
                return false;

            // Validate each test case in the tests array
            foreach (var testElement in testsElement.EnumerateArray())
            {
                if (!testElement.TryGetProperty("name", out _))
                    return false;

                if (!testElement.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
                    return false;
            }

            return true;
        }
    }
}
