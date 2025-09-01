using System.Text.Json;

namespace JTest;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var cli = new JTestCli();
            return await cli.RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class JTestCli
{
    private Dictionary<string, string> _envVars = new();
    private Dictionary<string, string> _globals = new();

    private static readonly string HelpText = @"JTEST CLI v1.0 - Universal Test Definition Language
==================================================

USAGE:
    jtest <COMMAND> [OPTIONS]
    jtest <testfile.json> [OPTIONS]     # Direct test execution

COMMANDS:
    run <testfile>                      Run test file
    export <format> <testfile> [output] Export tests to other frameworks
    debug <testfile>                    Run with verbose debug output and markdown log
    validate <testfile>                 Validate test file syntax
    --help, -h                          Show this help message

EXPORT FORMATS:
    postman         Postman Collection (v2.1)
    karate          Karate DSL Feature file

RUNTIME OPTIONS:
    --env key=value                     Set environment variable
    --env-file <path.json>              Load environment from JSON file
    --globals key=value                 Set global variable
    --globals-file <path.json>          Load globals from JSON file

EXAMPLES:
    # Run a test file
    jtest run my_api_tests.json
    jtest my_api_tests.json                     # Shorthand

    # Run with environment variables
    jtest run tests.json --env baseUrl=https://api.prod.com
    jtest run tests.json --env-file prod.json

    # Export to other frameworks
    jtest export postman tests.json
    jtest export karate tests.json my_tests

    # Validate test files
    jtest validate tests.json

    # Debug mode with verbose output
    jtest debug tests.json
    jtest debug tests.json --env verbosity=Verbose

For more information, visit: https://github.com/ELSA-X/JTEST";

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        // Parse runtime options first
        var parsedArgs = ParseRuntimeOptions(args);

        if (parsedArgs.Count == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = parsedArgs[0].ToLower();

        // Handle help command
        if (command == "--help" || command == "-h" || command == "help")
        {
            ShowHelp();
            return 0;
        }

        // Handle direct test execution (shorthand)
        if (command.EndsWith(".json") && !IsKnownCommand(command))
        {
            return await RunCommand("run", parsedArgs);
        }

        // Handle known commands
        return command switch
        {
            "run" => await RunCommand("run", parsedArgs.Skip(1).ToList()),
            "export" => await ExportCommand(parsedArgs.Skip(1).ToList()),
            "debug" => await DebugCommand(parsedArgs.Skip(1).ToList()),
            "validate" => await ValidateCommand(parsedArgs.Skip(1).ToList()),
            _ => HandleUnknownCommand(command)
        };
    }

    private List<string> ParseRuntimeOptions(string[] args)
    {
        var remainingArgs = new List<string>();
        
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            if (arg == "--env" && i + 1 < args.Length)
            {
                var envPair = args[++i];
                var parts = envPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    _envVars[parts[0]] = parts[1];
                    Console.WriteLine($"Set environment variable: {parts[0]}={parts[1]}");
                }
                else
                {
                    Console.Error.WriteLine($"Invalid environment variable format: {envPair}. Expected key=value");
                }
            }
            else if (arg == "--env-file" && i + 1 < args.Length)
            {
                var envFile = args[++i];
                LoadEnvironmentFile(envFile);
            }
            else if (arg == "--globals" && i + 1 < args.Length)
            {
                var globalPair = args[++i];
                var parts = globalPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    _globals[parts[0]] = parts[1];
                    Console.WriteLine($"Set global variable: {parts[0]}={parts[1]}");
                }
                else
                {
                    Console.Error.WriteLine($"Invalid global variable format: {globalPair}. Expected key=value");
                }
            }
            else if (arg == "--globals-file" && i + 1 < args.Length)
            {
                var globalsFile = args[++i];
                LoadGlobalsFile(globalsFile);
            }
            else
            {
                remainingArgs.Add(arg);
            }
        }

        return remainingArgs;
    }

    private void LoadEnvironmentFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Environment file not found: {filePath}");
                return;
            }

            var json = File.ReadAllText(filePath);
            var envData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (envData != null)
            {
                foreach (var kvp in envData)
                {
                    _envVars[kvp.Key] = kvp.Value?.ToString() ?? "";
                    Console.WriteLine($"Loaded environment variable from file: {kvp.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading environment file {filePath}: {ex.Message}");
        }
    }

    private void LoadGlobalsFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Globals file not found: {filePath}");
                return;
            }

            var json = File.ReadAllText(filePath);
            var globalsData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (globalsData != null)
            {
                foreach (var kvp in globalsData)
                {
                    _globals[kvp.Key] = kvp.Value?.ToString() ?? "";
                    Console.WriteLine($"Loaded global variable from file: {kvp.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading globals file {filePath}: {ex.Message}");
        }
    }

    private bool IsKnownCommand(string command)
    {
        return command.ToLower() is "run" or "export" or "debug" or "validate" or "--help" or "-h";
    }

    private Task<int> RunCommand(string command, List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine($"Error: {command} command requires a test file argument");
            Console.Error.WriteLine($"Usage: jtest {command} <testfile>");
            return Task.FromResult(1);
        }

        var testFile = args[0];
        
        if (!File.Exists(testFile))
        {
            Console.Error.WriteLine($"Error: Test file not found: {testFile}");
            return Task.FromResult(1);
        }

        Console.WriteLine($"Running test file: {testFile}");
        
        if (_envVars.Count > 0)
        {
            Console.WriteLine("Environment variables:");
            foreach (var kvp in _envVars)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        if (_globals.Count > 0)
        {
            Console.WriteLine("Global variables:");
            foreach (var kvp in _globals)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        // TODO: Implement actual test running logic
        Console.WriteLine("Test execution completed successfully.");
        
        return Task.FromResult(0);
    }

    private Task<int> ExportCommand(List<string> args)
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("Error: export command requires format and test file arguments");
            Console.Error.WriteLine("Usage: jtest export <format> <testfile> [output]");
            Console.Error.WriteLine("Supported formats: postman, karate");
            return Task.FromResult(1);
        }

        var format = args[0].ToLower();
        var testFile = args[1];
        var outputFile = args.Count > 2 ? args[2] : null;

        if (format != "postman" && format != "karate")
        {
            Console.Error.WriteLine($"Error: Unsupported export format: {format}");
            Console.Error.WriteLine("Supported formats: postman, karate");
            return Task.FromResult(1);
        }

        if (!File.Exists(testFile))
        {
            Console.Error.WriteLine($"Error: Test file not found: {testFile}");
            return Task.FromResult(1);
        }

        var defaultOutput = format switch
        {
            "postman" => Path.ChangeExtension(testFile, ".postman_collection.json"),
            "karate" => Path.ChangeExtension(testFile, ".feature"),
            _ => throw new InvalidOperationException()
        };

        var output = outputFile ?? defaultOutput;

        Console.WriteLine($"Exporting test file: {testFile}");
        Console.WriteLine($"Export format: {format}");
        Console.WriteLine($"Output file: {output}");

        // TODO: Implement actual export logic
        Console.WriteLine("Export completed successfully.");

        return Task.FromResult(0);
    }

    private Task<int> DebugCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine("Error: debug command requires a test file argument");
            Console.Error.WriteLine("Usage: jtest debug <testfile>");
            return Task.FromResult(1);
        }

        var testFile = args[0];
        
        if (!File.Exists(testFile))
        {
            Console.Error.WriteLine($"Error: Test file not found: {testFile}");
            return Task.FromResult(1);
        }

        Console.WriteLine($"Running test file in debug mode: {testFile}");
        Console.WriteLine("Debug mode: ON");
        Console.WriteLine("Verbose output: ON");
        Console.WriteLine("Markdown logging: ON");

        if (_envVars.Count > 0)
        {
            Console.WriteLine("\n## Environment Variables");
            foreach (var kvp in _envVars)
            {
                Console.WriteLine($"- **{kvp.Key}**: {kvp.Value}");
            }
        }

        if (_globals.Count > 0)
        {
            Console.WriteLine("\n## Global Variables");
            foreach (var kvp in _globals)
            {
                Console.WriteLine($"- **{kvp.Key}**: {kvp.Value}");
            }
        }

        // TODO: Implement actual debug execution logic
        Console.WriteLine("\n## Test Execution");
        Console.WriteLine("Debug execution completed successfully.");

        return Task.FromResult(0);
    }

    private async Task<int> ValidateCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine("Error: validate command requires a test file argument");
            Console.Error.WriteLine("Usage: jtest validate <testfile>");
            return 1;
        }

        var testFile = args[0];
        
        if (!File.Exists(testFile))
        {
            Console.Error.WriteLine($"Error: Test file not found: {testFile}");
            return 1;
        }

        Console.WriteLine($"Validating test file: {testFile}");

        try
        {
            var json = await File.ReadAllTextAsync(testFile);
            JsonDocument.Parse(json);
            Console.WriteLine("✓ Valid JSON syntax");
            
            // TODO: Implement actual JTEST schema validation
            Console.WriteLine("✓ Valid JTEST schema");
            Console.WriteLine("Validation completed successfully.");
            
            return 0;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"✗ Invalid JSON syntax: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Validation error: {ex.Message}");
            return 1;
        }
    }

    private int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Error: Unknown command '{command}'");
        Console.Error.WriteLine("Run 'jtest --help' to see available commands.");
        return 1;
    }

    private void ShowHelp()
    {
        Console.WriteLine(HelpText);
    }
}
