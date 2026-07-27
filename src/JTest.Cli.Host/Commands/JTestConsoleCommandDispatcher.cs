using JTest.Cli.Commands;
using JTest.Cli.Composition;
using JTest.Cli.Invocation;

namespace GeneratedHost.Commands;

/// <summary>
/// Maps the generated parse result onto the jtest command router. The
/// returned integer is the process exit code, unchanged.
/// </summary>
internal sealed class JTestConsoleCommandDispatcher : IProgramKitConsoleCommandDispatcher
{
    public async ValueTask<int> DispatchAsync(
        GeneratedConsoleParseResult parseResult,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var router = CliComposition.CreateRouter(httpClient);

        var invocation = new CliInvocation(
            parseResult.Command,
            parseResult.Options,
            parseResult.Arguments);
        var environment = new CliEnvironment(
            Directory.GetCurrentDirectory(),
            Environment.GetEnvironmentVariable("CI"),
            Console.IsOutputRedirected);

        return await router.Execute(invocation, environment, cancellationToken)
            .ConfigureAwait(false);
    }
}
