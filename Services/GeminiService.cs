using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageAIRenamer.Services
{
    public class GeminiService
    {
        private readonly string[] _apiKeys;
        private int _currentKeyIndex = 0;
        private readonly HttpClient _httpClient;
        private const string Model = "gemini-2.0-flash"; 
        
        private const string PromptText = "Extract only the main title or name from this image. Return ONLY the title with no descriptions, explanations, or extra text. If there is readable text like a game name or service name use it exactly. If no clear text exists, generate a short appropriate title. Return only the title.";

        public GeminiService(string[] apiKeys)
        {
            _apiKeys = apiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateTitleAsync(string imagePath, string? customInstructions = null)
        {
            if (_apiKeys.Length == 0) throw new Exception("No API keys provided.");

            int attempts = 0;
            int maxAttempts = _apiKeys.Length;

            while (attempts < maxAttempts)
            {
                string apiKey = _apiKeys[_currentKeyIndex];
                try
                {
                    return await CallApiAsync(apiKey, imagePath, customInstructions);
                }
                catch (Exception ex)
                {
                    // Check for quota errors (429 Too Many Requests or specific error messages)
                    string msg = ex.Message.ToLower();
                    if (msg.Contains("429") || msg.Contains("quota") || msg.Contains("resource_exhausted") || msg.Contains("rate limit"))
                    {
                        _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Length;
                        attempts++;
                        if (attempts >= maxAttempts) throw new Exception("All API keys exhausted.", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new Exception("Failed to generate title.");
        }

        private async Task<string> CallApiAsync(string apiKey, string imagePath, string? customInstructions)
        {
            var bytes = await File.ReadAllBytesAsync(imagePath);
            var base64 = Convert.ToBase64String(bytes);
            var mimeType = GetMimeType(Path.GetExtension(imagePath));

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={apiKey}";

            var finalPrompt = PromptText;
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

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            
            return text?.Trim() ?? throw new Exception("Empty response from API");
        }

        private string GetMimeType(string ext) => ext.ToLower() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg"
        };
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[]? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public Part[]? Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
