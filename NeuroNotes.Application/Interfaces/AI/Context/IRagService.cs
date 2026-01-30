
namespace NeuroNotes.Application.Interfaces.AI.Context
{
    public interface IRagService
    {
        Task<string> GetRelevantContextAsync(string userId, string query, CancellationToken cancellationToken);
    }
}
