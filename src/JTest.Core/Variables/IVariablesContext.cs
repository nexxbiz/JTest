namespace JTest.Core.Variables;

public interface IVariablesContext
{
    IReadOnlyDictionary<string, object?> GlobalVariables { get; }

    IReadOnlyDictionary<string, object?> EnvironmentVariables { get; }

    /// <summary>
    /// Generated values that are unique to this run and stable for its whole duration, exposed as
    /// <c>$.run</c>. They let a suite create server-side resources with globally-unique identity and
    /// stay re-runnable. Stable (not per-reference) so a create step and a later fetch step agree
    /// without an intervening <c>save</c>.
    /// </summary>
    IReadOnlyDictionary<string, object?> RunVariables { get; }

    void Initialize(IReadOnlyDictionary<string, object?>? env, IReadOnlyDictionary<string, object?>? globals);
}
