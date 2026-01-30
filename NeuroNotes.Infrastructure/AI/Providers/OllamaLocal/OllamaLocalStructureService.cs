using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NeuroNotes.Infrastructure.AI.Providers.OllamaLocal
{
    public class OllamaLocalStructureService : IStructureService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaLocalOptions _defaultOptions;
        private readonly ILogger<OllamaLocalStructureService> _logger;

        private static readonly Regex MarkdownBlockRegex = new(
            @"^```(?:json|xml|markdown|md|text)?\s*(.*?)\s*```$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public OllamaLocalStructureService(
            HttpClient httpClient,
            IOptions<OllamaLocalOptions> options,
            ILogger<OllamaLocalStructureService> logger)
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
            var structureModel = providerSettings is not null && providerSettings.TryGetValue("StructureModel", out var sm) && !string.IsNullOrWhiteSpace(sm)
                ? sm
                : _defaultOptions.StructureModel;

            var temperature = 0.2;
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

            if (string.IsNullOrWhiteSpace(structureModel))
            {
                _logger.LogError("Ollama Model is missing.");
                throw new InvalidOperationException("Ollama Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogDebug("Sending structure request to Ollama. Model: {Model}, BaseUrl: {BaseUrl}", structureModel, baseUrl);

            var requestBody = new
            {
                model = structureModel,
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = rawText }
                },
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
                var text = result?.Message?.Content;

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Ollama returned an empty structure result.");
                    return string.Empty;
                }

                return CleanMarkdown(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Ollama structure generation. URL: {Url}", url);
                throw;
            }
        }

        private string CleanMarkdown(string text)
        {
            var match = MarkdownBlockRegex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
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
