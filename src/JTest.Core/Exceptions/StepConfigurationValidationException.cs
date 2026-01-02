namespace JTest.Core.Exceptions;

public sealed class StepConfigurationValidationException(string type, IEnumerable<string> validationErrors)
    : Exception(ParseValidationErrorMessage(type, validationErrors))
{
    private static string ParseValidationErrorMessage(string type, IEnumerable<string> validationErrors)
    {
        var errors = string.Join("; ", validationErrors);
        return $"Invalid configuration for step type '{type}'. Validation errors: {errors}";
    }
}
