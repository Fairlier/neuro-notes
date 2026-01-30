using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Interfaces.AI.Embeddings
{
    public interface IEmbeddingModelFactory
    {
        IEmbeddingService Create(); 
        IEmbeddingService Create(EmbeddingProviderType provider);
    }
}
