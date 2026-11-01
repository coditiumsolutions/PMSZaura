using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Salzee.Services
{
    public class GroqAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _apiKeys;
        private int _currentKeyIndex = 0;
        private readonly object _lockObject = new object();

        public GroqAIService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            _apiKeys = configuration.GetSection("Groq:ApiKeys").Get<string[]>()
                ?? Array.Empty<string>();
        }

        private string GetNextApiKey()
        {
            lock (_lockObject)
            {
                var key = _apiKeys[_currentKeyIndex];
                _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Length;
                return key;
            }
        }

        public async Task<string> ChatAsync(string userMessage, string systemPrompt)
        {
            var apiKey = GetNextApiKey();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = "openai/gpt-oss-120b",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.1,
                max_tokens = 2000
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response";
            }
            catch (Exception ex)
            {
                throw new Exception($"Groq API error: {ex.Message}", ex);
            }
        }
    }
}
