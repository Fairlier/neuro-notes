
namespace NeuroNotes.Application.Interfaces.AI.Providers.Services
{
    public interface ISummaryService
    {
        Task<string> SummarizeAsync(
            string structureText,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken);
    }
}
