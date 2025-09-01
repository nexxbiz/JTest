using JTest.Core;

// JTest CLI Application
var testRunner = new TestRunner();

Console.WriteLine(testRunner.GetWelcomeMessage());
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine("Usage: JTest.Cli <json-file-path>");
    Console.WriteLine("       JTest.Cli --version");
    return 1;
}

if (args[0] == "--version")
{
    Console.WriteLine($"Version: {testRunner.Version}");
    return 0;
}

string jsonFilePath = args[0];
if (!File.Exists(jsonFilePath))
{
    Console.WriteLine($"Error: File '{jsonFilePath}' not found.");
    return 1;
}

try
{
    string jsonContent = await File.ReadAllTextAsync(jsonFilePath);
    bool isValid = testRunner.ValidateTestDefinition(jsonContent);
    
    if (isValid)
    {
        Console.WriteLine($"✓ Test definition '{jsonFilePath}' is valid.");
    }
    else
    {
        Console.WriteLine($"✗ Test definition '{jsonFilePath}' is invalid.");
        return 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error reading file: {ex.Message}");
    return 1;
}

return 0;
