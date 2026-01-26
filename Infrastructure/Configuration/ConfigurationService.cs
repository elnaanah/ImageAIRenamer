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
    public int GetRequestDelaySeconds()
    {
        var val = _configuration["Gemini:RequestDelaySeconds"];
        if (int.TryParse(val, out int result) && result >= 0)
        {
            return result;
        }
        return 2; // Default delay
    }

    /// <inheritdoc/>
    public string[] GetSupportedExtensions()
    {
        var extensions = _configuration.GetSection("Application:SupportedExtensions").Get<string[]>();
        return extensions ?? new[] { ".jpg", ".jpeg", ".png", ".webp" };
    }

    /// <summary>
    /// Gets the default prompt text (for backward compatibility)
    /// </summary>
    public string GetDefaultPrompt()
    {
        return _configuration["Gemini:DefaultPrompt"] ?? string.Empty;
    }

    /// <inheritdoc/>
    public string GetRenamePrompt()
    {
        return _configuration["Gemini:RenamePrompt"] ?? "Extract only the main title or name from this image. Return ONLY the title with no descriptions, explanations, or extra text. If there is readable text like a service name use it exactly. If no clear text exists, generate a short appropriate title. Return only the title.";
    }

    /// <inheritdoc/>
    public string GetSearchPrompt()
    {
        return _configuration["Gemini:SearchPrompt"] ?? "Analyze this image and extract a filename. You can provide a brief description if needed. If there is readable text in the image (like app name, title, label), use it exactly as the filename. If no clear text exists, generate a short descriptive filename (2-5 words maximum).";
    }
}
