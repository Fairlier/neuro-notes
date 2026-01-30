using NeuroNotes.Domain.Enums;
using Pgvector;

namespace NeuroNotes.Application.Interfaces.AI.Embeddings
{
    public interface IEmbeddingService
    {
        Task<Vector> GenerateEmbeddingAsync(string text, EmbeddingType type, CancellationToken cancellationToken);

        Task<List<Vector>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, EmbeddingType type, CancellationToken cancellationToken);

        int EmbeddingDimension { get; }
    }
}
