using JTest.Cli.Loading;

namespace JTest.Cli.Console;

/// <summary>
/// Host-side pre-checks for the run request: suite discovery must match at
/// least one file and the run inputs must bind. Messages are the exact texts
/// the run command reports for the same conditions.
/// </summary>
public sealed class JTestRunValidator : IJTestRunValidator
{
    private readonly IJTestCliSession session;

    /// <summary>Creates the validator.</summary>
    /// <param name="session">Ambient session facts.</param>
    public JTestRunValidator(IJTestCliSession session)
    {
        this.session = session;
    }

    /// <inheritdoc />
    public ValueTask<JTestConsoleValidationResult> ValidateAsync(
        JTestRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var invocation = JTestInvocationMapper.Map(request);
        var environment = session.Capture();

        var files = SuiteFileDiscovery.Resolve(environment.WorkingDirectory, invocation.Arguments);
        if (files.Count == 0)
        {
            return Invalid($"No suite files matched: {string.Join(", ", invocation.Arguments)}");
        }

        return RunInputBinder.TryBind(invocation, environment, out _, out var error)
            ? ValueTask.FromResult(new JTestConsoleValidationResult(true, []))
            : Invalid(error);
    }

    private static ValueTask<JTestConsoleValidationResult> Invalid(string message) =>
        ValueTask.FromResult(new JTestConsoleValidationResult(false, [message]));
}
