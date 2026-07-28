namespace JTest.Cli.Console;

/// <summary>
/// Validates the typed <c>jtest run</c> request before the handler executes:
/// the host prints the returned messages and exits with the invalid-invocation
/// exit code when the result is invalid.
/// </summary>
public interface IJTestRunValidator
{
    /// <summary>Validates the request.</summary>
    /// <param name="request">The typed request.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    ValueTask<JTestConsoleValidationResult> ValidateAsync(
        JTestRunRequest request,
        CancellationToken cancellationToken);
}
