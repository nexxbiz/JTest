using JTest.Engine.Ports;

namespace JTest.Engine.Tests.TestSupport;

/// <summary>Delay scheduler that records requests and completes immediately.</summary>
internal sealed class NoDelayScheduler : IDelayScheduler
{
    internal List<TimeSpan> Requested { get; } = [];

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requested.Add(duration);
        return Task.CompletedTask;
    }
}
