using JTest.Core.Utilities;

namespace JTest.Core.Exceptions;

/// <summary>
/// A <c>{{...}}</c> token in a step field referenced a path that matched nothing.
///
/// This is an error rather than an empty substitution because the silent version produces false
/// positives: a suite that mints a unique resource name per run instead sends a constant (e.g. the
/// literal route "greet-"), so every run targets the same server-side resource and can pass while
/// being served by a previous run's artifact. A step must never send a request built from a value the
/// suite did not actually produce.
/// </summary>
public sealed class UnresolvedTokenException(string expression, IReadOnlyList<string> unresolvedPaths)
    : Exception(BuildMessage(expression, unresolvedPaths))
{
    public string Expression { get; } = expression;
    public IReadOnlyList<string> UnresolvedPaths { get; } = unresolvedPaths;

    private static string BuildMessage(string expression, IReadOnlyList<string> unresolvedPaths)
    {
        var details = string.Join(" ", unresolvedPaths.Select(VariableInterpolator.DescribeUnresolvedPath));

        return $"Unresolved token in '{expression}'. {details} " +
               "A step field is never filled in with an empty string for a path that matched nothing, " +
               "because that silently changes what the step does.";
    }
}
