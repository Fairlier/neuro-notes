using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Infrastructure.AI.Embeddings
{
    public class EmbeddingModelFactory : IEmbeddingModelFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EmbeddingOptions _options;
        private readonly ILogger<EmbeddingModelFactory> _logger;

        public EmbeddingModelFactory(
            IServiceProvider serviceProvider,
            IOptions<EmbeddingOptions> options,
            ILogger<EmbeddingModelFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        public IEmbeddingService Create()
        {
            return Create(_options.DefaultEmbeddingProvider);
        }

        public IEmbeddingService Create(EmbeddingProviderType provider)
        {
            _logger.LogDebug(
                "Creating embedding service for provider: {Provider}", 
                provider);

            try
            {
                return provider switch
                {
                    EmbeddingProviderType.GemmaLocal => _serviceProvider.GetRequiredService<GemmaLocalEmbeddingService>(),
                    _ => throw new NotSupportedException(
                        $"Embedding provider '{provider}' is not supported or not registered in DI.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create embedding service for provider {Provider}.", provider);
                throw;
            }
        }
    }
}
