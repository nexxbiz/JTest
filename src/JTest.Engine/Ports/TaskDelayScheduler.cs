namespace JTest.Engine.Ports;

/// <summary>The real <see cref="Task.Delay(TimeSpan, CancellationToken)"/> scheduler.</summary>
public sealed class TaskDelayScheduler : IDelayScheduler
{
    /// <inheritdoc />
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken) =>
        Task.Delay(duration, cancellationToken);
}
