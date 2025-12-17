using System.IO;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ImageAIRenamer.Infrastructure.Configuration;

// Expose this class to allow access to GetDefaultPrompt

/// <summary>
/// Service for managing application configuration including API keys
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string _apiKeysFilePath;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "ImageAIRenamer");
        Directory.CreateDirectory(folder);
        _apiKeysFilePath = Path.Combine(folder, "apikeys.txt");
    }

    /// <inheritdoc/>
    public async Task<string[]> GetApiKeysAsync()
    {
        try
        {
            if (File.Exists(_apiKeysFilePath))
            {
                var content = await File.ReadAllTextAsync(_apiKeysFilePath);
                return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToArray();
            }
        }
        catch
        {
            // Return empty array on error
        }
        return Array.Empty<string>();
    }

    /// <inheritdoc/>
    public async Task SaveApiKeysAsync(string[] apiKeys)
    {
        try
        {
            var content = string.Join(Environment.NewLine, apiKeys.Where(k => !string.IsNullOrWhiteSpace(k)));
            await File.WriteAllTextAsync(_apiKeysFilePath, content);
        }
        catch
        {
            // Silently fail - could log here
        }
    }

    /// <inheritdoc/>
    public string GetGeminiModel()
    {
        return _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
    }

    /// <inheritdoc/>
    public string[] GetSupportedExtensions()
    {
        var extensions = _configuration.GetSection("Application:SupportedExtensions").Get<string[]>();
        return extensions ?? new[] { ".jpg", ".jpeg", ".png", ".webp" };
    }

    /// <summary>
    /// Gets the default prompt text
    /// </summary>
    public string GetDefaultPrompt()
    {
        return _configuration["Gemini:DefaultPrompt"] ?? string.Empty;
    }
}
