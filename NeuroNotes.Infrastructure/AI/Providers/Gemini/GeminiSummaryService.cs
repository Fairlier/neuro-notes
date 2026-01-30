using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NeuroNotes.Infrastructure.AI.Providers.Gemini
{
    public class GeminiSummaryService : ISummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _defaultOptions;
        private readonly ILogger<GeminiSummaryService> _logger;

        private static readonly Regex MarkdownBlockRegex = new(
            @"^```(?:markdown|md|text)?\s*(.*?)\s*```$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public GeminiSummaryService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiSummaryService> logger)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
        }

        public async Task<string> SummarizeAsync(
            string structureText,
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
                _logger.LogError("Gemini API Key is missing.");
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Gemini BaseUrl is empty.");
                throw new InvalidOperationException("Gemini BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(summaryModel))
            {
                _logger.LogError("Gemini Model is missing.");
                throw new InvalidOperationException("Gemini Model is not configured.");
            }

            _logger.LogDebug("Sending summarization request to Gemini. Model: {Model}", summaryModel);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = structureText } }
                    }
                },
                generationConfig = new { temperature = temperature }
            };

            baseUrl = baseUrl.TrimEnd('/');
            var url = $"{baseUrl}/v1beta/models/{summaryModel}:generateContent?key={apiKey}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini Summary request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Gemini Summary Failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
                var resultText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(resultText))
                {
                    _logger.LogWarning("Gemini returned empty summary result.");
                    return string.Empty;
                }

                return CleanMarkdown(resultText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Gemini summarization.");
                throw;
            }
        }

        private string CleanMarkdown(string text)
        {
            var match = MarkdownBlockRegex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        private class GeminiResponse
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
