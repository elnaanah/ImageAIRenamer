using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace ImageAIRenamer.Infrastructure.Services;

public class GeminiService : IGeminiService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GeminiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private int _currentKeyIndex = 0;
    private string[] _apiKeys = Array.Empty<string>();
    private int _parallelKeyIndex = 0;

    public GeminiService(
        IConfigurationService configurationService,
        ILogger<GeminiService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configurationService = configurationService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public void SetApiKeys(string[] apiKeys)
    {
        _apiKeys = apiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
        _currentKeyIndex = 0;
        _parallelKeyIndex = 0;
        _logger.LogInformation("API keys configured. {Count} keys available.", _apiKeys.Length);
    }

    /// <inheritdoc/>
    public string GetApiKeyForIndex(int index)
    {
        if (_apiKeys.Length == 0)
        {
            throw new InvalidOperationException("No API keys configured.");
        }
        if (index < 0 || index >= _apiKeys.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }
        return _apiKeys[index];
    }

    /// <inheritdoc/>
    public int ApiKeysCount => _apiKeys.Length;

    /// <inheritdoc/>
    public int GetNextApiKeyIndex()
    {
        if (_apiKeys.Length == 0)
        {
            return 0;
        }
        var nextIndex = Interlocked.Increment(ref _parallelKeyIndex);
        return (nextIndex - 1) % _apiKeys.Length;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateTitleAsync(string imagePath, string? customInstructions = null, CancellationToken cancellationToken = default)
    {
        return await GenerateTitleAsync(imagePath, customInstructions, null, cancellationToken);
    }

    public async Task<string> GenerateTitleAsync(string imagePath, string? customInstructions, int? apiKeyIndex, CancellationToken cancellationToken = default)
    {
        if (_apiKeys.Length == 0)
        {
            _logger.LogError("No API keys configured");
            throw new InvalidOperationException("No API keys provided.");
        }

        var model = _configurationService.GetGeminiModel();
        var defaultPrompt = ((ConfigurationService)_configurationService).GetDefaultPrompt();

        int attempts = 0;
        int maxAttempts = _apiKeys.Length;
        int currentAttemptIndex = apiKeyIndex.HasValue ? apiKeyIndex.Value : _currentKeyIndex;

        while (attempts < maxAttempts)
        {
            string apiKey = _apiKeys[currentAttemptIndex % _apiKeys.Length];
            try
            {
                _logger.LogDebug("Generating title for image: {ImagePath} using key index {Index}", imagePath, currentAttemptIndex % _apiKeys.Length);
                return await CallGenerateApiAsync(apiKey, imagePath, customInstructions, model, defaultPrompt, cancellationToken);
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();
                if (msg.Contains("429") || msg.Contains("quota") || msg.Contains("resource_exhausted") || msg.Contains("rate limit"))
                {
                    _logger.LogWarning("API key {Index} quota exceeded, rotating to next key", currentAttemptIndex % _apiKeys.Length);
                    currentAttemptIndex++;
                    attempts++;
                    
                    // If we are in sequential mode (no specific index requested), update the global index
                    if (!apiKeyIndex.HasValue)
                    {
                        _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Length;
                    }
                    
                    if (attempts >= maxAttempts)
                    {
                        _logger.LogError("All API keys exhausted");
                        throw new InvalidOperationException("All API keys exhausted.", ex);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error generating title for image: {ImagePath}", imagePath);
                    throw;
                }
            }
        }
        throw new InvalidOperationException("Failed to generate title.");
    }

    /// <inheritdoc/>
    public async Task<SearchResult> SearchImageAsync(string imagePath, string searchDescription, CancellationToken cancellationToken = default)
    {
        return await SearchImageAsync(imagePath, searchDescription, null, cancellationToken);
    }

    public async Task<SearchResult> SearchImageAsync(string imagePath, string searchDescription, int? apiKeyIndex, CancellationToken cancellationToken = default)
    {
        if (_apiKeys.Length == 0)
        {
            _logger.LogError("No API keys configured");
            throw new InvalidOperationException("No API keys provided.");
        }

        var model = _configurationService.GetGeminiModel();

        int attempts = 0;
        int maxAttempts = _apiKeys.Length;
        int currentAttemptIndex = apiKeyIndex.HasValue ? apiKeyIndex.Value : _currentKeyIndex;

        while (attempts < maxAttempts)
        {
            string apiKey = _apiKeys[currentAttemptIndex % _apiKeys.Length];
            try
            {
                _logger.LogDebug("Searching image: {ImagePath} for: {Description} using key index {Index}", imagePath, searchDescription, currentAttemptIndex % _apiKeys.Length);
                return await CallSearchApiAsync(apiKey, imagePath, searchDescription, model, cancellationToken);
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();
                if (msg.Contains("429") || msg.Contains("quota") || msg.Contains("resource_exhausted") || msg.Contains("rate limit"))
                {
                    _logger.LogWarning("API key {Index} quota exceeded, rotating to next key", currentAttemptIndex % _apiKeys.Length);
                    currentAttemptIndex++;
                    attempts++;

                    // If we are in sequential mode (no specific index requested), update the global index
                    if (!apiKeyIndex.HasValue)
                    {
                        _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Length;
                    }

                    if (attempts >= maxAttempts)
                    {
                        _logger.LogError("All API keys exhausted");
                        throw new InvalidOperationException("All API keys exhausted.", ex);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error searching image: {ImagePath}", imagePath);
                    throw;
                }
            }
        }
        throw new InvalidOperationException("Failed to search image.");
    }

    private async Task<string> CallGenerateApiAsync(string apiKey, string imagePath, string? customInstructions, string model, string defaultPrompt, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        
        var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = GetMimeType(Path.GetExtension(imagePath));

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var finalPrompt = defaultPrompt;
        if (!string.IsNullOrWhiteSpace(customInstructions))
        {
            finalPrompt += $"\n\nAdditional User Instructions: {customInstructions}";
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = finalPrompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.0,
                maxOutputTokens = 50
            }
        };

        var response = await httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("API Error {StatusCode}: {Error}", response.StatusCode, error);
            throw new HttpRequestException($"API Error {response.StatusCode}: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty response from API for image: {ImagePath}", imagePath);
            throw new InvalidOperationException("Empty response from API");
        }

        return text.Trim();
    }

    private async Task<SearchResult> CallSearchApiAsync(string apiKey, string imagePath, string searchDescription, string model, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        
        var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = GetMimeType(Path.GetExtension(imagePath));

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var searchPrompt = $@"Analyze this image and determine if it contains or shows: {searchDescription}

Respond with a JSON object in this exact format:
{{
  ""isMatch"": true or false,
  ""suggestedName"": ""appropriate filename based on image content"",
  ""reason"": ""brief explanation of why it matches or not""
}}

If the image matches the description, set isMatch to true and generate an appropriate filename. If it doesn't match, set isMatch to false.
Return ONLY the JSON object, no other text.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = searchPrompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 200
            }
        };

        var response = await httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("API Error {StatusCode}: {Error}", response.StatusCode, error);
            throw new HttpRequestException($"API Error {response.StatusCode}: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty response from API for image search: {ImagePath}", imagePath);
            throw new InvalidOperationException("Empty response from API");
        }

        // Try to parse JSON response
        try
        {
            // Extract JSON from response (might have markdown code blocks)
            var jsonText = text.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            var searchResult = JsonSerializer.Deserialize<SearchResult>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (searchResult != null)
            {
                _logger.LogDebug("Successfully parsed search result for image: {ImagePath}, IsMatch: {IsMatch}", imagePath, searchResult.IsMatch);
                return searchResult;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response, attempting fallback parsing");
            // If JSON parsing fails, try to extract information from text
            var lowerText = text.ToLower();
            var isMatch = lowerText.Contains("ismatch") && (lowerText.Contains("\"true\"") || lowerText.Contains(":true"));
            
            return new SearchResult
            {
                IsMatch = isMatch,
                SuggestedName = null,
                Reason = text.Length > 200 ? text.Substring(0, 200) : text
            };
        }

        // Fallback: try to determine match from text content
        var lowerResponse = text.ToLower();
        var containsMatch = lowerResponse.Contains("match") || lowerResponse.Contains("yes") || lowerResponse.Contains("true");
        
        return new SearchResult
        {
            IsMatch = containsMatch,
            SuggestedName = null,
            Reason = text
        };
    }

    private static string GetMimeType(string ext) => ext.ToLower() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "image/jpeg"
    };
}

/// <summary>
/// Response model for Gemini API
/// </summary>
internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public Candidate[]? Candidates { get; set; }
}

/// <summary>
/// Candidate model for Gemini API response
/// </summary>
internal class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

/// <summary>
/// Content model for Gemini API response
/// </summary>
internal class Content
{
    [JsonPropertyName("parts")]
    public Part[]? Parts { get; set; }
}

/// <summary>
/// Part model for Gemini API response
/// </summary>
internal class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
