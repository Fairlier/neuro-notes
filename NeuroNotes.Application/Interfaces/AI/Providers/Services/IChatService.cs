using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Interfaces.AI.Providers.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(
            string userMessage,
            IEnumerable<ChatMessage> history,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken);
    }
}
