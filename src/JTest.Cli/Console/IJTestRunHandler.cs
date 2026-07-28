namespace JTest.Cli.Console;

/// <summary>Handles the typed <c>jtest run</c> request.</summary>
public interface IJTestRunHandler
{
    /// <summary>Executes the run and returns the process exit code.</summary>
    /// <param name="request">The typed request.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    ValueTask<int> HandleAsync(
        JTestRunRequest request,
        CancellationToken cancellationToken);
}
