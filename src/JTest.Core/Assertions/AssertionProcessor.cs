using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.Core.Assertions;

/// <summary>
/// Default implementation of IAssertionProcessor
/// </summary>
public sealed class AssertionProcessor : IAssertionProcessor
{
    public Task<IEnumerable<AssertionResult>> ProcessAssertionsAsync(IEnumerable<IAssertionOperation> assertOperations, IExecutionContext context)
    {
        var results = new List<AssertionResult>();

        foreach (var operation in assertOperations)
        {
            var result = ProcessSingleAssertion(operation, context);
            result.SetMask(operation.Mask);

            results.Add(result);
        }

        return Task.FromResult(results.AsEnumerable());
    }

    private static AssertionResult ProcessSingleAssertion(IAssertionOperation operation, IExecutionContext context)
    {
        // Resolve any variable tokens in the description
        var resolvedDescription = VariableInterpolator.ResolveVariableTokens(operation.Description ?? string.Empty, context);
        var description = resolvedDescription?.ToString() ?? string.Empty;

        var result = operation.Execute(context);

        // Set the description on the result
        result.Description = description;

        return result;
    }
}