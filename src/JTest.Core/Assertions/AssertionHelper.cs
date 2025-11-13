namespace JTest.Core.Assertions
{
    internal static class AssertionHelper
    {
        internal static bool IsDateTimeAssertion(object? actualValue, object? expectedValue, out long actualTicks, out long expectedTicks)
        {
            actualTicks = 0;
            expectedTicks = 0;

            if (actualValue is string actualValueString && expectedValue is string expectedValueString)
            {
                if (DateTimeOffset.TryParse(actualValueString, out var actualDateTime) && DateTimeOffset.TryParse(expectedValueString, out var expectedDateTime))
                {
                    actualTicks = actualDateTime.ToUniversalTime().Ticks;
                    expectedTicks = expectedDateTime.ToUniversalTime().Ticks;
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
