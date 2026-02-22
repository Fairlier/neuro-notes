using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Providers.Gemini
{
    public class GeminiChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _defaultOptions;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
        }

        public async Task<string> SendMessageAsync(
            string userMessage,
            IEnumerable<ChatMessage> history,
            string systemInstruction,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken)
        {
            var apiKey = providerSettings is not null && providerSettings.TryGetValue("ApiKey", out var key) && !string.IsNullOrWhiteSpace(key)
                ? key
                : _defaultOptions.ApiKey;

            var chatModel = providerSettings is not null && providerSettings.TryGetValue("ChatModel", out var mdl) && !string.IsNullOrWhiteSpace(mdl)
                ? mdl
                : _defaultOptions.GlobalChatModel;

            var temperature = 0.7;
            if (providerSettings is not null && providerSettings.TryGetValue("Temperature", out var tempStr) &&
                double.TryParse(tempStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedTemp))
            {
                temperature = parsedTemp;
            }

            var baseUrl = _defaultOptions.BaseUrl;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini API Key is missing. Check user profile settings or global configuration.");
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Gemini BaseUrl is empty.");
                throw new InvalidOperationException("Gemini BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(chatModel))
            {
                _logger.LogError("Gemini Model is missing.");
                throw new InvalidOperationException("Gemini Model is not configured.");
            }

            _logger.LogDebug("Sending message to Gemini. Model: {Model}", chatModel);

            var contents = new List<object>();

            foreach (var msg in history)
            {
                contents.Add(new
                {
                    role = msg.Role == ChatRole.User ? "user" : "model",
                    parts = new[] { new { text = msg.Content } }
                });
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = contents.ToArray(),
                generationConfig = new { temperature = temperature }
            };

            baseUrl = baseUrl.TrimEnd('/');
            var url = $"{baseUrl}/v1beta/models/{chatModel}:generateContent?key={apiKey}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Gemini API Error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<GeminiChatResponse>(cancellationToken);
                var responseText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("Gemini returned an empty response or unexpected JSON structure.");
                    return string.Empty;
                }

                return responseText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while communicating with Gemini API.");
                throw;
            }
        }


        private class GeminiChatResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate>? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part>? Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
