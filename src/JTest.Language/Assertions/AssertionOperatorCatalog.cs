namespace JTest.Language.Assertions;

/// <summary>
/// The closed set of assertion operators and their operand requirements.
/// The language manifest is generated from the same facts recorded here.
/// </summary>
public static class AssertionOperatorCatalog
{
    private static readonly Dictionary<string, (bool RequiresActual, bool RequiresExpected)> Shapes =
        new(StringComparer.Ordinal)
        {
            ["equals"] = (true, true),
            ["notEquals"] = (true, true),
            ["contains"] = (true, true),
            ["notContains"] = (true, true),
            ["exists"] = (true, false),
            ["notExists"] = (true, false),
            ["greaterThan"] = (true, true),
            ["lessThan"] = (true, true),
            ["greaterOrEqual"] = (true, true),
            ["lessOrEqual"] = (true, true),
            ["between"] = (true, true),
            ["in"] = (true, true),
            ["matches"] = (true, true),
            ["startsWith"] = (true, true),
            ["endsWith"] = (true, true),
            ["length"] = (true, true),
            ["empty"] = (true, false),
            ["notEmpty"] = (true, false),
            ["type"] = (true, true),
        };

    /// <summary>All operator names in ordinal order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        Shapes.Keys.Order(StringComparer.Ordinal).ToArray();

    /// <summary>Returns whether the operator exists.</summary>
    /// <param name="name">Operator name.</param>
    public static bool IsKnown(string name) => Shapes.ContainsKey(name);

    /// <summary>Returns whether the operator requires the <c>actual</c> operand.</summary>
    /// <param name="name">Operator name.</param>
    public static bool RequiresActual(string name) =>
        Shapes.TryGetValue(name, out var shape) && shape.RequiresActual;

    /// <summary>Returns whether the operator requires the <c>expected</c> operand.</summary>
    /// <param name="name">Operator name.</param>
    public static bool RequiresExpected(string name) =>
        Shapes.TryGetValue(name, out var shape) && shape.RequiresExpected;
}
