using JTest.Cli.Settings;
using Spectre.Console.Cli;

namespace JTest.UnitTests.Cli;

/// <summary>
/// Regression cover for issue #74: `-e/--env` values never reached `$.env` because the option did
/// not bind. These tests drive the real Spectre parser, so they fail if the binding breaks again —
/// asserting on <see cref="RunCommandSettings.GetEnvironmentVariables"/> alone would not.
/// </summary>
public class EnvironmentVariableOptionTests
{
    [Fact]
    public void SingleEnvOption_ReachesEnvironmentVariables()
    {
        var env = ParseEnvironment("suite.json", "-e", "baseUrl=http://localhost:2");

        Assert.Equal("http://localhost:2", env["baseUrl"]);
    }

    [Fact]
    public void LongFormEnvOption_ReachesEnvironmentVariables()
    {
        var env = ParseEnvironment("suite.json", "--env", "token=abc");

        Assert.Equal("abc", env["token"]);
    }

    [Fact]
    public void RepeatedEnvOptions_AllReachEnvironmentVariables()
    {
        var env = ParseEnvironment("suite.json", "-e", "a=1", "-e", "b=2", "-e", "c=3");

        Assert.Equal(3, env.Count);
        Assert.Equal("1", env["a"]);
        Assert.Equal("2", env["b"]);
        Assert.Equal("3", env["c"]);
    }

    [Fact]
    public void ValueContainingEqualsSign_IsKeptWhole()
    {
        // Only the first '=' separates key from value; connection strings and base64 end in '='.
        var env = ParseEnvironment("suite.json", "-e", "conn=Server=db;Pwd=p@ss=");

        Assert.Equal("Server=db;Pwd=p@ss=", env["conn"]);
    }

    [Fact]
    public void EmptyValue_IsAnEmptyStringNotTheKey()
    {
        var env = ParseEnvironment("suite.json", "-e", "baseUrl=");

        Assert.Equal(string.Empty, env["baseUrl"]);
    }

    [Fact]
    public void LastOccurrenceOfADuplicateKeyWins()
    {
        var env = ParseEnvironment("suite.json", "-e", "baseUrl=first", "-e", "baseUrl=second");

        Assert.Equal("second", env["baseUrl"]);
    }

    [Fact]
    public void EnvOptionWithoutSeparator_IsAValidationError()
    {
        var settings = new RunCommandSettings
        {
            TestFilePatterns = ["suite.json"],
            EnvironmentVariables = ["baseUrl"]
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
        Assert.Contains("key=value", result.Message);
    }

    [Fact]
    public void NoEnvOptions_LeavesEnvironmentVariablesUnset()
    {
        var settings = Parse("suite.json");

        Assert.Null(settings.GetEnvironmentVariables());
    }

    [Fact]
    public void EnvOptionOverridesEnvFileForTheSameKey()
    {
        var file = WriteTempEnvFile("""{ "baseUrl": "http://from-file", "shared": "file" }""");
        try
        {
            var env = ParseEnvironment(
                "suite.json", "--env-file", file, "-e", "baseUrl=http://from-cli");

            // A key supplied by both must not throw (it used to Add over an existing key), and the
            // command line is the more specific source, so it wins.
            Assert.Equal("http://from-cli", env["baseUrl"]);
            Assert.Equal("file", Text(env["shared"]));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void EnvFileAloneStillPopulatesEnvironmentVariables()
    {
        var file = WriteTempEnvFile("""{ "baseUrl": "http://from-file" }""");
        try
        {
            var env = ParseEnvironment("suite.json", "--env-file", file);

            Assert.Equal("http://from-file", Text(env["baseUrl"]));
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>Values read from --env-file arrive as <c>JsonElement</c>; compare them as text.</summary>
    private static string? Text(object? value) => value?.ToString();

    private static string WriteTempEnvFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"jtest-env-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static IReadOnlyDictionary<string, object?> ParseEnvironment(params string[] args)
    {
        var env = Parse(args).GetEnvironmentVariables();
        Assert.NotNull(env);
        return env;
    }

    private static RunCommandSettings Parse(params string[] args)
    {
        CaptureSettingsCommand.Captured = null;

        var app = new CommandApp<CaptureSettingsCommand>();
        app.Configure(config => config.PropagateExceptions());

        Assert.Equal(0, app.Run(args));
        Assert.NotNull(CaptureSettingsCommand.Captured);

        return CaptureSettingsCommand.Captured;
    }

    private sealed class CaptureSettingsCommand : Command<RunCommandSettings>
    {
        public static RunCommandSettings? Captured;

        public override int Execute(CommandContext context, RunCommandSettings settings, CancellationToken cancellationToken)
        {
            Captured = settings;
            return 0;
        }
    }
}
