namespace JTest.Cli.Console;

/// <summary>The typed <c>jtest describe</c> request.</summary>
public sealed class JTestDescribeRequest
{
    /// <summary>Creates the request.</summary>
    /// <param name="schema">Schema selector: manifest, suite, templates, or result.</param>
    /// <param name="output">Output path, or <c>-</c> for standard output.</param>
    public JTestDescribeRequest(string schema, string output)
    {
        Schema = schema;
        Output = output;
    }

    /// <summary>Gets the schema selector.</summary>
    public string Schema { get; }

    /// <summary>Gets the output path, or <c>-</c> for standard output.</summary>
    public string Output { get; }
}
