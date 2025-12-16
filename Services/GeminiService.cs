using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageAIRenamer.Models;

namespace ImageAIRenamer.Services
{
    public class GeminiService
    {
        private readonly string[] _apiKeys;
        private int _currentKeyIndex = 0;
        private readonly HttpClient _httpClient;
        private const string Model = "gemini-2.0-flash"; 
        
        private const string PromptText = "Extract only the main title or name from this image. Return ONLY the title with no descriptions, explanations, or extra text. If there is readable text like a service name use it exactly. If no clear text exists, generate a short appropriate title. Return only the title.";

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

        public async Task<SearchResult> SearchImageAsync(string imagePath, string searchDescription)
        {
            if (_apiKeys.Length == 0) throw new Exception("No API keys provided.");

            int attempts = 0;
            int maxAttempts = _apiKeys.Length;

            while (attempts < maxAttempts)
            {
                string apiKey = _apiKeys[_currentKeyIndex];
                try
                {
                    return await CallSearchApiAsync(apiKey, imagePath, searchDescription);
                }
                catch (Exception ex)
                {
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
            throw new Exception("Failed to search image.");
        }

        private async Task<SearchResult> CallSearchApiAsync(string apiKey, string imagePath, string searchDescription)
        {
            var bytes = await File.ReadAllBytesAsync(imagePath);
            var base64 = Convert.ToBase64String(bytes);
            var mimeType = GetMimeType(Path.GetExtension(imagePath));

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={apiKey}";

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

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Empty response from API");
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
                    return searchResult;
                }
            }
            catch (JsonException)
            {
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
