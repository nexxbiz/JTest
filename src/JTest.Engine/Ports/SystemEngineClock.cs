namespace JTest.Engine.Ports;

/// <summary>The real system clock.</summary>
public sealed class SystemEngineClock : IEngineClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
