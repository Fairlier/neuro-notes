namespace NeuroNotes.Application.Interfaces.AI.Providers.Services
{
    public interface ITranscriptionService
    {
        Task<string> TranscribeAsync(
            Stream audioStream,
            string fileName,
            string contentType,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken);
    }
}
