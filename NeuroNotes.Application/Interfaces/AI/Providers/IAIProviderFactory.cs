using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Interfaces.AI.Providers
{
    public interface IAIProviderFactory
    {
        ITranscriptionService GetTranscriptionService(TranscriptionProviderType provider);
        IStructureService GetStructureService(StructureProviderType provider);
        ISummaryService GetSummaryService(SummaryProviderType provider);
        IChatService GetGlobalChatService(ChatProviderType provider);
        IChatService GetNoteChatService(ChatProviderType provider);

        ITranscriptionService GetTranscriptionService();
        IStructureService GetStructureService();
        ISummaryService GetSummaryService();
        IChatService GetGlobalChatService();
        IChatService GetNoteChatService();
    }
}
