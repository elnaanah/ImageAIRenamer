using ImageAIRenamer.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace ImageAIRenamer.Tests.Unit.Services;

/// <summary>
/// Unit tests for ConfigurationService
/// </summary>
public class ConfigurationServiceTests
{
    private ConfigurationService CreateService(Dictionary<string, string?>? configValues = null)
    {
        var builder = new ConfigurationBuilder();
        
        if (configValues != null)
        {
            builder.AddInMemoryCollection(configValues);
        }

        var configuration = builder.Build();
        return new ConfigurationService(configuration);
    }

    #region GetGeminiModel Tests

    [Fact]
    public void GetGeminiModel_WithConfiguredValue_ReturnsConfiguredValue()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            { "Gemini:Model", "gemini-pro" }
        };
        var service = CreateService(config);

        // Act
        var result = service.GetGeminiModel();

        // Assert
        Assert.Equal("gemini-pro", result);
    }

    [Fact]
    public void GetGeminiModel_WithNoConfig_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetGeminiModel();

        // Assert
        Assert.Equal("gemini-2.0-flash", result);
    }

    #endregion

    #region GetSupportedExtensions Tests

    [Fact]
    public void GetSupportedExtensions_WithNoConfig_ReturnsDefaults()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetSupportedExtensions();

        // Assert
        Assert.Contains(".jpg", result);
        Assert.Contains(".jpeg", result);
        Assert.Contains(".png", result);
        Assert.Contains(".webp", result);
    }

    [Fact]
    public void GetSupportedExtensions_WithConfiguredValues_ReturnsConfigured()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            { "Application:SupportedExtensions:0", ".gif" },
            { "Application:SupportedExtensions:1", ".bmp" }
        };
        var service = CreateService(config);

        // Act
        var result = service.GetSupportedExtensions();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains(".gif", result);
        Assert.Contains(".bmp", result);
    }

    #endregion

    #region GetDefaultPrompt Tests

    [Fact]
    public void GetDefaultPrompt_WithNoConfig_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetDefaultPrompt();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetDefaultPrompt_WithConfiguredValue_ReturnsConfiguredValue()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            { "Gemini:DefaultPrompt", "Custom prompt" }
        };
        var service = CreateService(config);

        // Act
        var result = service.GetDefaultPrompt();

        // Assert
        Assert.Equal("Custom prompt", result);
    }

    #endregion

    #region API Keys Tests

    [Fact]
    public async Task SaveAndGetApiKeys_RoundTrip_Success()
    {
        // Arrange
        var service = CreateService();
        var keys = new[] { "key1", "key2", "key3" };

        // Act
        await service.SaveApiKeysAsync(keys);
        var result = await service.GetApiKeysAsync();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("key1", result);
        Assert.Contains("key2", result);
        Assert.Contains("key3", result);
    }

    [Fact]
    public async Task SaveApiKeys_WithEmptyKeys_SavesEmpty()
    {
        // Arrange
        var service = CreateService();
        
        // First save some keys
        await service.SaveApiKeysAsync(new[] { "key1" });
        
        // Then save empty
        await service.SaveApiKeysAsync(Array.Empty<string>());

        // Act
        var result = await service.GetApiKeysAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveApiKeys_FiltersWhitespaceKeys()
    {
        // Arrange
        var service = CreateService();
        var keys = new[] { "key1", "", "  ", "key2" };

        // Act
        await service.SaveApiKeysAsync(keys);
        var result = await service.GetApiKeysAsync();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("key1", result);
        Assert.Contains("key2", result);
    }

    #endregion
}
