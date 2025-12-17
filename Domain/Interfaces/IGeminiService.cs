namespace ImageAIRenamer.Domain.Interfaces;

/// <summary>
/// Service interface for interacting with Google Gemini AI API
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Generates a title/name for an image using AI
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="customInstructions">Optional custom instructions for the AI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated title</returns>
    Task<string> GenerateTitleAsync(string imagePath, string? customInstructions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for images matching a description
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="searchDescription">Description of what to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search result indicating if the image matches</returns>
    Task<Domain.Entities.SearchResult> SearchImageAsync(string imagePath, string searchDescription, CancellationToken cancellationToken = default);
}
