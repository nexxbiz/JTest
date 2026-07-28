namespace JTest.Cli.Console;

/// <summary>
/// The console validation result contract the generated host consumes:
/// validity plus the exact messages to print before the invalid-invocation
/// exit code is returned.
/// </summary>
public sealed class JTestConsoleValidationResult
{
    /// <summary>Creates the result.</summary>
    /// <param name="isValid">Whether the request may execute.</param>
    /// <param name="messages">Messages explaining an invalid request.</param>
    public JTestConsoleValidationResult(
        bool isValid,
        IReadOnlyList<string> messages)
    {
        IsValid = isValid;
        Messages = messages;
    }

    /// <summary>Gets whether the request may execute.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the messages explaining an invalid request.</summary>
    public IReadOnlyList<string> Messages { get; }
}
