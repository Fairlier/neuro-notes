using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Providers.Mistral
{
    public class MistralTranscriptionService : ITranscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly MistralOptions _defaultOptions;
        private readonly ILogger<MistralTranscriptionService> _logger;

        public MistralTranscriptionService(
            HttpClient httpClient,
            IOptions<MistralOptions> options,
            ILogger<MistralTranscriptionService> logger)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
        }

        public async Task<string> TranscribeAsync(
            Stream audioStream,
            string fileName,
            string contentType,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken)
        {
            var apiKey = providerSettings is not null && providerSettings.TryGetValue("ApiKey", out var key) && !string.IsNullOrWhiteSpace(key)
                ? key
                : _defaultOptions.ApiKey;

            var transcriptionModel = providerSettings is not null && providerSettings.TryGetValue("TranscriptionModel", out var mdl) && !string.IsNullOrWhiteSpace(mdl)
                ? mdl
                : _defaultOptions.TranscriptionModel;

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

            if (string.IsNullOrWhiteSpace(transcriptionModel))
            {
                _logger.LogError("Mistral Model is missing.");
                throw new InvalidOperationException("Mistral Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogDebug("Starting Mistral transcription. File: {FileName}, Model: {Model}", fileName, transcriptionModel);

            if (audioStream.CanSeek)
            {
                audioStream.Position = 0;
            }

            using var content = new MultipartFormDataContent
            {
                { new StringContent(transcriptionModel), "model" }
            };

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                content.Add(new StringContent(systemPrompt), "prompt");
            }

            if (providerSettings is not null && providerSettings.TryGetValue("Language", out var lang) && !string.IsNullOrWhiteSpace(lang))
            {
                content.Add(new StringContent(lang), "language");
            }

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var url = $"{baseUrl}/audio/transcriptions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = content;

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Mistral Transcription failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Mistral Transcription Failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<MistralTranscriptionResponse>(cancellationToken);

                if (string.IsNullOrWhiteSpace(result?.Text))
                {
                    _logger.LogWarning("Mistral returned empty transcription text.");
                    return string.Empty;
                }

                return result.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Mistral transcription request.");
                throw;
            }
        }

        private class MistralTranscriptionResponse
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
