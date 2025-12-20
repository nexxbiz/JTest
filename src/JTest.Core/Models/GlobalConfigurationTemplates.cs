namespace JTest.Core.Models;

public sealed record GlobalConfigurationTemplates(
    IEnumerable<string>? SearchPaths,
    IEnumerable<string>? Paths
);
