using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NeuroNotes.Infrastructure.AI.Providers.Mistral
{
    public class MistralSummaryService : ISummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly MistralOptions _defaultOptions;
        private readonly ILogger<MistralSummaryService> _logger;

        private static readonly Regex MarkdownBlockRegex = new(
            @"^```(?:json|xml|markdown|md|text)?\s*(.*?)\s*```$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MistralSummaryService(
            HttpClient httpClient,
            IOptions<MistralOptions> options,
            ILogger<MistralSummaryService> logger)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
        }

        public async Task<string> SummarizeAsync(
            string text,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken)
        {
            var apiKey = providerSettings is not null && providerSettings.TryGetValue("ApiKey", out var key) && !string.IsNullOrWhiteSpace(key)
                ? key
                : _defaultOptions.ApiKey;

            var summaryModel = providerSettings is not null && providerSettings.TryGetValue("SummaryModel", out var mdl) && !string.IsNullOrWhiteSpace(mdl)
                ? mdl
                : _defaultOptions.SummaryModel;

            var temperature = 0.5;
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
                _logger.LogError("Mistral BaseUrl is empty.");
                throw new InvalidOperationException("Mistral BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(summaryModel))
            {
                _logger.LogError("Mistral Model is missing.");
                throw new InvalidOperationException("Mistral Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogDebug("Sending summarization request to Mistral. Model: {Model}", summaryModel);

            var requestBody = new
            {
                model = summaryModel,
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
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
                    _logger.LogError("Mistral Summary request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Mistral API Error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<MistralResponse>(cancellationToken);
                var resultText = result?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(resultText))
                {
                    _logger.LogWarning("Mistral returned empty summary result.");
                    return string.Empty;
                }

                return CleanMarkdown(resultText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Mistral summarization.");
                throw;
            }
        }

        private string CleanMarkdown(string text)
        {
            var match = MarkdownBlockRegex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        private class MistralResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice>? Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")]
            public Message? Message { get; set; }
        }

        private class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
