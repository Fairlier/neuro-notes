using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Interfaces.AI.Embeddings
{
    public interface INoteEmbeddingGenerator
    {
        Task GenerateAndSaveEmbeddingsAsync(Note note, CancellationToken cancellationToken);
    }
}
