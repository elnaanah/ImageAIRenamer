namespace ImageAIRenamer.Domain.Interfaces;

/// <summary>
/// Service interface for application configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the API keys from storage
    /// </summary>
    /// <returns>Array of API keys</returns>
    Task<string[]> GetApiKeysAsync();

    /// <summary>
    /// Saves API keys to storage
    /// </summary>
    /// <param name="apiKeys">Array of API keys to save</param>
    Task SaveApiKeysAsync(string[] apiKeys);

    /// <summary>
    /// Gets the Gemini model name from configuration
    /// </summary>
    string GetGeminiModel();

    /// <summary>
    /// Gets supported image file extensions
    /// </summary>
    string[] GetSupportedExtensions();
}
