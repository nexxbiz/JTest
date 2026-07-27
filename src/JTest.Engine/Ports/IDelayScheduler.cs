namespace JTest.Engine.Ports;

/// <summary>Delay primitive used by wait steps and loop pacing.</summary>
public interface IDelayScheduler
{
    /// <summary>Waits for the given duration.</summary>
    /// <param name="duration">The delay duration.</param>
    /// <param name="cancellationToken">Cancels the delay.</param>
    Task Delay(TimeSpan duration, CancellationToken cancellationToken);
}
