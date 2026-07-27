namespace JTest.Engine.Execution;

/// <summary>Options of one run.</summary>
/// <param name="Parallelism">Maximum suites executed concurrently; 1 means sequential.</param>
/// <param name="ExtraEnv">CLI-provided env values merged over suite env (CLI wins).</param>
/// <param name="ExtraGlobals">CLI-provided global values merged over suite globals (CLI wins).</param>
/// <param name="ExtraSecretEnvNames">Names of CLI env entries whose values are sensitive.</param>
public sealed record RunOptions(
    int Parallelism = 1,
    IReadOnlyDictionary<string, string>? ExtraEnv = null,
    IReadOnlyDictionary<string, string>? ExtraGlobals = null,
    IReadOnlyList<string>? ExtraSecretEnvNames = null);
