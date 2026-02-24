
namespace NeuroNotes.Application.Interfaces.AI.Embeddings
{
    public interface INoteSearchService
    {
        Task<List<Guid>> SemanticSearchAsync(
            string userId,
            string query,
            int limit,
            CancellationToken cancellationToken);

        Task<List<Guid>> TextSearchAsync(
            string userId,
            string query,
            int limit,
            CancellationToken cancellationToken);
    }
}
