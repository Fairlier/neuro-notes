using Microsoft.EntityFrameworkCore;
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
    public class GeminiStructureService : IStructureService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _defaultOptions;
        private readonly ILogger<GeminiStructureService> _logger;

        private static readonly Regex MarkdownBlockRegex = new(
            @"^```(?:markdown|md)?\s*(.*?)\s*```$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public GeminiStructureService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiStructureService> logger)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
        }

        public async Task<string> StructureAsync(
            string rawText,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken)
        {
            var apiKey = providerSettings is not null && providerSettings.TryGetValue("ApiKey", out var key) && !string.IsNullOrWhiteSpace(key)
                ? key
                : _defaultOptions.ApiKey;

            var structureModel = providerSettings is not null && providerSettings.TryGetValue("StructureModel", out var mdl) && !string.IsNullOrWhiteSpace(mdl)
                ? mdl
                : _defaultOptions.StructureModel;

            var temperature = 0.2;
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

            if (string.IsNullOrWhiteSpace(structureModel))
            {
                _logger.LogError("Gemini Model is missing.");
                throw new InvalidOperationException("Gemini Model is not configured.");
            }

            _logger.LogDebug("Sending structure request to Gemini. Model: {Model}", structureModel);

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
                        parts = new[] { new { text = rawText } }
                    }
                },
                generationConfig = new { temperature = temperature }
            };

            baseUrl = baseUrl.TrimEnd('/');
            var url = $"{baseUrl}/v1beta/models/{structureModel}:generateContent?key={apiKey}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini Structure request failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Gemini Structure Failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
                var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Gemini returned empty structure result.");
                    return string.Empty;
                }

                return CleanMarkdown(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Gemini structure generation.");
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
