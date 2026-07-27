using JTest.Engine.Ports;

namespace JTest.Engine.Tests.TestSupport;

/// <summary>Deterministic clock: every read advances by a fixed step.</summary>
internal sealed class FixedClock : IEngineClock
{
    private DateTimeOffset current;

    internal FixedClock(DateTimeOffset start)
    {
        current = start;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            var value = current;
            current = current.AddMilliseconds(10);
            return value;
        }
    }
}
