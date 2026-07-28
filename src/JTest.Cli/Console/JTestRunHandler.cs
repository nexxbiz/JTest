using JTest.Cli.Commands;

namespace JTest.Cli.Console;

/// <summary>
/// Adapts the typed run request onto the existing command router, which
/// keeps the frozen exit-code and exception semantics authoritative.
/// </summary>
public sealed class JTestRunHandler : IJTestRunHandler
{
    private readonly CliCommandRouter router;
    private readonly IJTestCliSession session;

    /// <summary>Creates the handler.</summary>
    /// <param name="router">The jtest command router.</param>
    /// <param name="session">Ambient session facts.</param>
    public JTestRunHandler(CliCommandRouter router, IJTestCliSession session)
    {
        this.router = router;
        this.session = session;
    }

    /// <inheritdoc />
    public async ValueTask<int> HandleAsync(
        JTestRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await router.Execute(
            JTestInvocationMapper.Map(request),
            session.Capture(),
            cancellationToken).ConfigureAwait(false);
    }
}
