namespace JTest.Core.Models;

public sealed record GlobalConfiguration(
    GlobalConfigurationTemplates? Templates = null,
    string? OutputDirectory = null
);
