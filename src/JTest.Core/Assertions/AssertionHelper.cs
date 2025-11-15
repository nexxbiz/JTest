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

                if (TimeOnly.TryParse(actualValueString, out var actualTimeOnly) && TimeOnly.TryParse(expectedValueString, out var expectedTimeOnly))
                {
                    actualTicks = actualTimeOnly.Ticks;
                    expectedTicks = expectedTimeOnly.Ticks;
                    return true;
                }

                return false;
            }

            if(actualValue is DateTime actualValueDateTime && expectedValue is DateTime expectedValueDateTime)
            {
                actualTicks = actualValueDateTime.ToUniversalTime().Ticks;
                expectedTicks = expectedValueDateTime.ToUniversalTime().Ticks;
                return true;
            }

            if (actualValue is DateOnly actualValueDateOnly && expectedValue is DateOnly expectedValueDateOnly)
            {
                actualTicks = actualValueDateOnly.ToDateTime(TimeOnly.MinValue).Ticks;
                expectedTicks = expectedValueDateOnly.ToDateTime(TimeOnly.MinValue).Ticks;
                return true;
            }

            if (actualValue is TimeOnly actualValueTimeOnly && expectedValue is TimeOnly expectedValueTimeOnly)
            {
                actualTicks = actualValueTimeOnly.Ticks;
                expectedTicks = expectedValueTimeOnly.Ticks;
                return true;
            }

            if (actualValue is DateTimeOffset actualValueDateTimeOffset && expectedValue is DateTimeOffset expectedValueDateTimeOffset)
            {
                actualTicks = actualValueDateTimeOffset.ToUniversalTime().Ticks;
                expectedTicks = expectedValueDateTimeOffset.ToUniversalTime().Ticks;
                return true;
            }

            return false;
        }
    }
}
