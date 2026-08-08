using JTest.Core.TypeDescriptors;

namespace JTest.Core.Assertions;

/// <summary>
/// The built-in assertion operators, discovered from the <see cref="IAssertionOperation"/>
/// implementations using the same identification rule the execution registry uses. Validation resolves
/// operators through here rather than a hand-maintained list, so <c>jtest validate</c> cannot drift
/// from what a run accepts (FR-055).
///
/// This covers the operators shipped with JTest. Operators contributed at run time by a registered
/// assembly are resolved against the live registry instead, at the point of deserialization.
/// </summary>
public static class AssertionOperators
{
    private static readonly Lazy<SortedSet<string>> Operators = new(Discover);

    /// <summary>All supported operator names, ordered, in their canonical lower-case spelling.</summary>
    public static IReadOnlyCollection<string> All => Operators.Value;

    /// <summary>
    /// Whether the operator resolves. Matching is case-insensitive, exactly as at run time — so a
    /// spelling that executes today is never rejected by validation.
    /// </summary>
    public static bool IsKnown(string? op) =>
        !string.IsNullOrWhiteSpace(op) && Operators.Value.Contains(op);

    private static SortedSet<string> Discover()
    {
        var identification = new AssertionTypeDescriptorIdentification();
        var marker = typeof(IAssertionOperation);

        var names = marker.Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && marker.IsAssignableFrom(type) && type != marker)
            .Select(identification.Identify);

        return new SortedSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }
}
