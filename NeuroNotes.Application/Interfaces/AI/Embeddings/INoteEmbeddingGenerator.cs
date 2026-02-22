using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Interfaces.AI.Embeddings
{
    public interface INoteEmbeddingGenerator
    {
        Task GenerateAndSaveEmbeddingsAsync(Note note, CancellationToken cancellationToken);

        Task UpdateEmbeddingsForSourceAsync(Note note, NoteChunkSourceType sourceType,
            CancellationToken cancellationToken);
    }
}
