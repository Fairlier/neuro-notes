
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Providers.Gemini
{
    public class GeminiTranscriptionService : ITranscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _defaultOptions;
        private readonly ILogger<GeminiTranscriptionService> _logger;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public GeminiTranscriptionService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiTranscriptionService> logger,
            RecyclableMemoryStreamManager streamManager)
        {
            _httpClient = httpClient;
            _defaultOptions = options.Value;
            _logger = logger;
            _streamManager = streamManager;
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

            var baseUrl = _defaultOptions.BaseUrl.TrimEnd('/');

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

            if (string.IsNullOrWhiteSpace(transcriptionModel))
            {
                _logger.LogError("Gemini Model is missing.");
                throw new InvalidOperationException("Gemini Model is not configured.");
            }

            baseUrl = baseUrl.TrimEnd('/');

            _logger.LogInformation("Starting transcription for {FileName}. Size: {Size}, Model: {Model}", fileName, audioStream.Length, transcriptionModel);

            try
            {
                return await TranscribeViaFileApiAsync(audioStream, fileName, contentType, systemPrompt, apiKey, transcriptionModel, baseUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini File API failed for {FileName}. Fallback to Inline Data.", fileName);

                if (audioStream.CanSeek)
                {
                    audioStream.Position = 0;
                }

                return await TranscribeViaInlineDataAsync(audioStream, contentType, systemPrompt, apiKey, transcriptionModel, baseUrl, cancellationToken);
            }
        }

        private async Task<string> TranscribeViaInlineDataAsync(
            Stream audioStream,
            string contentType,
            string prompt,
            string apiKey,
            string model,
            string baseUrl,
            CancellationToken cancellationToken)
        {
            using var memoryStream = _streamManager.GetStream("GeminiInline");
            await audioStream.CopyToAsync(memoryStream, cancellationToken);
            var base64Audio = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = prompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = contentType,
                                    data = base64Audio
                                }
                            }
                        }
                    }
                }
            };

            return await ExecuteGeminiRequestAsync(requestBody, apiKey, model, baseUrl, cancellationToken);
        }

        private async Task<string> TranscribeViaFileApiAsync(
            Stream audioStream,
            string fileName,
            string contentType,
            string prompt,
            string apiKey,
            string model,
            string baseUrl,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Uploading audio to Gemini File API: {FileName}", fileName);

            var uploadBaseUrl = baseUrl.Contains("generativelanguage.googleapis.com")
                ? baseUrl.Replace("generativelanguage.googleapis.com", "generativelanguage.googleapis.com/upload")
                : $"{baseUrl}/upload";

            var uploadUrl = $"{uploadBaseUrl}/v1beta/files?key={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("X-Goog-Upload-Protocol", "multipart");

            var metadataJson = JsonSerializer.Serialize(new { file = new { display_name = fileName } });

            var content = new MultipartContent("related")
            {
                new StringContent(metadataJson, System.Text.Encoding.UTF8, "application/json")
            };

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent);

            request.Content = content;

            using var uploadResponse = await _httpClient.SendAsync(request, cancellationToken);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var err = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini Upload Failed. Status: {StatusCode}, Details: {Error}", uploadResponse.StatusCode, err);
                throw new HttpRequestException($"Gemini File Upload Failed: {uploadResponse.StatusCode}");
            }

            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<GeminiFileUploadResponse>(cancellationToken);
            var fileNameId = uploadResult?.File?.Name;
            var fileUri = uploadResult?.File?.Uri;

            if (string.IsNullOrEmpty(fileUri) || string.IsNullOrEmpty(fileNameId))
            {
                throw new InvalidOperationException("Failed to retrieve File URI or Name from Gemini upload response.");
            }

            _logger.LogDebug("File uploaded. ID: {FileId}", fileNameId);

            try
            {
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = prompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new
                                {
                                    file_data = new
                                    {
                                        mime_type = contentType,
                                        file_uri = fileUri
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new { temperature = 0.0 }
                };

                return await ExecuteGeminiRequestAsync(requestBody, apiKey, model, baseUrl, cancellationToken);
            }
            finally
            {
                await DeleteGeminiFileAsync(fileNameId, apiKey, baseUrl);
            }
        }

        private async Task DeleteGeminiFileAsync(string fileNameId, string apiKey, string baseUrl)
        {
            try
            {
                var deleteUrl = $"{baseUrl}/v1beta/{fileNameId}?key={apiKey}";
                using var response = await _httpClient.DeleteAsync(deleteUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to delete temp file {FileId}. Status: {Status}. Error: {Error}", fileNameId, response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while deleting temp file {FileId}", fileNameId);
            }
        }

        private async Task<string> ExecuteGeminiRequestAsync(object requestBody, string apiKey, string model, string baseUrl, CancellationToken cancellationToken)
        {
            var url = $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";

            using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini GenerateContent Failed. Status: {StatusCode}, Details: {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Gemini API Error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private class GeminiFileUploadResponse
        {
            [JsonPropertyName("file")]
            public GeminiFile? File { get; set; }
        }

        private class GeminiFile
        {
            [JsonPropertyName("uri")]
            public string? Uri { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
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
