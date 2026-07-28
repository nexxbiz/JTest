namespace JTest.Cli.Console;

/// <summary>Handles the typed <c>jtest validate</c> request.</summary>
public interface IJTestValidateHandler
{
    /// <summary>Executes validation and returns the process exit code.</summary>
    /// <param name="request">The typed request.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    ValueTask<int> HandleAsync(
        JTestValidateRequest request,
        CancellationToken cancellationToken);
}
