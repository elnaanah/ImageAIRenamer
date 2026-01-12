namespace ImageAIRenamer.Domain.Entities;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public GeminiSettings Gemini { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
}

/// <summary>
/// Gemini AI configuration settings
/// </summary>
public class GeminiSettings
{
    public string Model { get; set; } = "gemini-2.0-flash";
    public string DefaultPrompt { get; set; } = string.Empty;
    public int RequestDelaySeconds { get; set; } = 2;
}

/// <summary>
/// Application-specific settings
/// </summary>
public class ApplicationSettings
{
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();
}
