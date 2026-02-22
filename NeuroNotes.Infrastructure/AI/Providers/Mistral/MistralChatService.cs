using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Providers.Mistral
{
    public class MistralChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly MistralOptions _defaultOptions;
        private readonly ILogger<MistralChatService> _logger;

        public MistralChatService(
            HttpClient httpClient,
            IOptions<MistralOptions> options,
            ILogger<MistralChatService> logger)
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
                _logger.LogError("Mistral API Key is missing.");
                throw new InvalidOperationException("Mistral API Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Mistral BaseUrl is empty. Check configuration.");
                throw new InvalidOperationException("Mistral BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(chatModel))
            {
                _logger.LogError("Mistral Model is missing.");
                throw new InvalidOperationException("Mistral Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogDebug("Sending chat message to Mistral. Model: {Model}, BaseUrl: {BaseUrl}", chatModel, baseUrl);

            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemInstruction))
            {
                messages.Add(new { role = "system", content = systemInstruction });
            }

            foreach (var msg in history)
            {
                messages.Add(new
                {
                    role = msg.Role == ChatRole.User ? "user" : "assistant",
                    content = msg.Content
                });
            }

            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = chatModel,
                messages = messages,
                temperature = temperature
            };

            var url = $"{baseUrl}/chat/completions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(requestBody);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Mistral API request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Mistral API Error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<MistralResponse>(cancellationToken);
                var responseText = result?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("Mistral returned an empty response.");
                    return string.Empty;
                }

                return responseText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while communicating with Mistral API. URL: {Url}", url);
                throw;
            }
        }

        private class MistralResponse
        {
            [JsonPropertyName("choices")]
            public List<MistralChoice>? Choices { get; set; }
        }

        private class MistralChoice
        {
            [JsonPropertyName("message")]
            public MistralMessage? Message { get; set; }
        }

        private class MistralMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
