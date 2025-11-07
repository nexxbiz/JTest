using JTest.Core.Debugging;
using Xunit;

namespace JTest.UnitTests;

public class SecurityMaskerTests
{
    [Fact]
    public void RegisterForMasking_WithSensitiveKeywords_MasksValues()
    {
        // Arrange
        var masker = new SecurityMasker();

        // Act & Assert - Test various security-sensitive keywords
        Assert.Equal("masked", masker.RegisterForMasking("password", "mypassword123"));
        Assert.Equal("masked", masker.RegisterForMasking("apiKey", "secret-api-key"));
        Assert.Equal("masked", masker.RegisterForMasking("authToken", "bearer-token-123"));
        Assert.Equal("masked", masker.RegisterForMasking("userSecret", "super-secret"));
        Assert.Equal("masked", masker.RegisterForMasking("credential", "user-credential"));
        Assert.Equal("masked", masker.RegisterForMasking("authorization", "Basic ABC123"));
        Assert.Equal("masked", masker.RegisterForMasking("bearer", "bearer xyz"));
    }

    [Fact]
    public void RegisterForMasking_WithNonSensitiveKeywords_DoesNotMaskValues()
    {
        // Arrange
        var masker = new SecurityMasker();

        // Act & Assert - Test non-sensitive keywords
        Assert.Equal("testuser", masker.RegisterForMasking("username", "testuser"));
        Assert.Equal("https://api.example.com", masker.RegisterForMasking("baseUrl", "https://api.example.com"));
        Assert.Equal("production", masker.RegisterForMasking("environment", "production"));
        Assert.Equal("5432", masker.RegisterForMasking("port", "5432"));
    }

    [Fact]
    public void RegisterForMasking_CaseInsensitive_MasksValues()
    {
        // Arrange
        var masker = new SecurityMasker();

        // Act & Assert - Test case-insensitive matching
        Assert.Equal("masked", masker.RegisterForMasking("API_KEY", "secret-key"));
        Assert.Equal("masked", masker.RegisterForMasking("Password", "secret-pass"));
        Assert.Equal("masked", masker.RegisterForMasking("AUTH_TOKEN", "token-value"));
        Assert.Equal("masked", masker.RegisterForMasking("database_SECRET", "db-secret"));
    }

    [Fact]
    public void ApplyMasking_ReplacesRegisteredValues_InText()
    {
        // Arrange
        var masker = new SecurityMasker();
        masker.RegisterForMasking("password", "secret123");
        masker.RegisterForMasking("apiKey", "key456");

        var text = "Password: secret123 and API Key: key456 in the debug output";

        // Act
        var result = masker.ApplyMasking(text);

        // Assert
        Assert.Contains("masked", result);
        Assert.DoesNotContain("secret123", result);
        Assert.DoesNotContain("key456", result);
    }

    [Fact]
    public void RegisterForMasking_WithStringValues_HandlesQuoting()
    {
        // Arrange
        var masker = new SecurityMasker();

        // Act
        var result = masker.RegisterForMasking("password", "secret123");

        // Assert
        Assert.Equal("masked", result);
        
        // Test that both quoted and unquoted versions are masked
        var textWithQuotes = "password: \"secret123\"";
        var textWithoutQuotes = "password: secret123";
        
        var maskedWithQuotes = masker.ApplyMasking(textWithQuotes);
        var maskedWithoutQuotes = masker.ApplyMasking(textWithoutQuotes);
        
        Assert.DoesNotContain("secret123", maskedWithQuotes);
        Assert.DoesNotContain("secret123", maskedWithoutQuotes);
    }

    [Fact]
    public void RegisterForMasking_WithNullOrEmptyValues_HandlesGracefully()
    {
        // Arrange
        var masker = new SecurityMasker();

        // Act & Assert
        Assert.Equal("null", masker.RegisterForMasking("password", null!));
        Assert.Equal("masked", masker.RegisterForMasking("password", ""));
        Assert.Equal("masked", masker.RegisterForMasking("password", "   "));
    }
}