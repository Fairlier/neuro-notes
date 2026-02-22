using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Providers.OllamaLocal
{
    public class OllamaLocalChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaLocalOptions _defaultOptions;
        private readonly ILogger<OllamaLocalChatService> _logger;

        public OllamaLocalChatService(
            HttpClient httpClient,
            IOptions<OllamaLocalOptions> options,
            ILogger<OllamaLocalChatService> logger)
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

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Ollama BaseUrl is empty. Check configuration.");
                throw new InvalidOperationException("Ollama BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(chatModel))
            {
                _logger.LogError("Ollama Model is missing.");
                throw new InvalidOperationException("Ollama Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogDebug("Sending chat message to Ollama. Model: {Model}, BaseUrl: {BaseUrl}", chatModel, baseUrl);

            var messages = new List<object>
            {
                new { role = "system", content = systemInstruction }
            };

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
                model  = chatModel,
                messages = messages,
                stream = false,
                options = new { temperature = temperature }
            };

            var url = $"{baseUrl}/api/chat";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Ollama API request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Ollama API Error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<OllamaLocalResponse>(cancellationToken);
                var responseText = result?.Message?.Content;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("Ollama returned an empty response.");
                    return string.Empty;
                }

                return responseText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while communicating with Ollama API. URL: {Url}", url);
                throw;
            }
        }

        private class OllamaLocalResponse
        {
            [JsonPropertyName("message")]
            public OllamaLocalMessage? Message { get; set; }
        }

        private class OllamaLocalMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
