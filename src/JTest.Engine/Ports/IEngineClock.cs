namespace JTest.Engine.Ports;

/// <summary>Wall clock used for trace timing evidence.</summary>
public interface IEngineClock
{
    /// <summary>The current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }
}
