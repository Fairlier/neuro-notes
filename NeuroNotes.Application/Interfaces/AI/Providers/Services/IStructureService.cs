namespace NeuroNotes.Application.Interfaces.AI.Providers.Services
{
    public interface IStructureService
    {
        Task<string> StructureAsync(
            string rawText,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken);
    }
}
