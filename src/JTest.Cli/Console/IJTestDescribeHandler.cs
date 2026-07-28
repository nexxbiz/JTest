namespace JTest.Cli.Console;

/// <summary>Handles the typed <c>jtest describe</c> request.</summary>
public interface IJTestDescribeHandler
{
    /// <summary>Executes describe and returns the process exit code.</summary>
    /// <param name="request">The typed request.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    ValueTask<int> HandleAsync(
        JTestDescribeRequest request,
        CancellationToken cancellationToken);
}
