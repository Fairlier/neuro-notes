using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Domain.Enums;
using Pgvector;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NeuroNotes.Infrastructure.AI.Embeddings
{
    public class GemmaLocalEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly GemmaLocalOptions _options;
        private readonly ILogger<GemmaLocalEmbeddingService> _logger;

        public int EmbeddingDimension => _options.Dimensions;

        public GemmaLocalEmbeddingService(
            HttpClient httpClient,
            IOptions<EmbeddingOptions> options,
            ILogger<GemmaLocalEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value.GemmaLocal;
            _logger = logger;
        }

        public async Task<Vector> GenerateEmbeddingAsync(string text, EmbeddingType type, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                _logger.LogError("GemmaLocal configuration is missing BaseUrl.");
                throw new InvalidOperationException("GemmaLocal settings are invalid: BaseUrl is required.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("GemmaLocal received empty text for embedding generation.");
                return new Vector(new float[_options.Dimensions]);
            }

            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/api/embeddings";

            string processedText = ApplyModelSpecificPrefix(text, type);

            var request = new
            {
                model = _options.EmbeddingModel,
                prompt = processedText,
                options = new
                {
                    num_ctx = 2048
                }
            };

            try
            {
                _logger.LogDebug("Sending request to GemmaLocal. Model: {Model}, Type: {Type}, Length: {Len}",
                    _options.EmbeddingModel, type, text.Length);

                var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogError(
                        "GemmaLocal API request failed. StatusCode: {Code}. Details: {Body}",
                        response.StatusCode, errorContent);

                    throw new HttpRequestException(
                        $"GemmaLocal Embedding API failed with status: " +
                        $"{response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<GemmaLocalEmbeddingResponse>(cancellationToken);

                if (result?.Embedding is null || result.Embedding.Length == 0)
                {
                    _logger.LogError("GemmaLocal API returned an empty embedding array. Response parsing might be incorrect.");
                    throw new InvalidOperationException("GemmaLocal returned invalid/empty embedding data.");
                }

                float[] vectorData = result.Embedding;

                if (vectorData.Length > _options.Dimensions)
                {
                    vectorData = vectorData.Take(_options.Dimensions).ToArray();
                }

                NormalizeVector(vectorData);

                return new Vector(vectorData);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while contacting GemmaLocal at {Url}", url);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GemmaLocalEmbeddingService.");
                throw;
            }
        }

        public async Task<List<Vector>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, EmbeddingType type, CancellationToken cancellationToken)
        {
            var textList = texts.ToList();
            _logger.LogInformation(
                "GemmaLocal generating batch embeddings for {Count} items.", 
                textList.Count);

            var tasks = textList.Select(text => GenerateEmbeddingAsync(text, type, cancellationToken));

            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }

        private string ApplyModelSpecificPrefix(string text, EmbeddingType type)
        {
            return type switch
            {
                EmbeddingType.Query => $"task: search result | query: {text}",
                EmbeddingType.Document => $"title: Document | text: {text}",
                _ => text
            };
        }

        private void NormalizeVector(float[] vector)
        {
            double sum = 0;
            for (int i = 0; i < vector.Length; i++) sum += vector[i] * vector[i];
            float magnitude = (float)Math.Sqrt(sum);

            if (magnitude < 1e-9) return;

            for (int i = 0; i < vector.Length; i++) vector[i] /= magnitude;
        }

        private class GemmaLocalEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[]? Embedding { get; set; }
        }
    }
}
