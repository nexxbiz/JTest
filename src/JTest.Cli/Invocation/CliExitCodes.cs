namespace JTest.Cli.Invocation;

/// <summary>The frozen jtest exit codes; computed from evidence, never guessed.</summary>
public static class CliExitCodes
{
    /// <summary>Every discovered suite produced a complete passing trace.</summary>
    public const int Passed = 0;

    /// <summary>At least one case failed, errored, timed out, or was cancelled.</summary>
    public const int TestsFailed = 1;

    /// <summary>Usage, input, discovery, or validation failure.</summary>
    public const int InvalidInput = 2;

    /// <summary>Unexpected internal failure.</summary>
    public const int InternalError = 3;
}
