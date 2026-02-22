using MediatR;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommand : IRequest
    {
        public string? AIOperationLanguage { get; set; }

        public TranscriptionProviderType? TranscriptionProvider { get; set; }
        public StructureProviderType? StructureProvider { get; set; }
        public SummaryProviderType? SummaryProvider { get; set; }
        public ChatProviderType? GlobalChatProvider { get; set; }
        public ChatProviderType? NoteChatProvider { get; set; }

        public Dictionary<string, Dictionary<string, string>>? ProviderSettings { get; set; }

        public AIOperationSettingsDto? Transcription { get; set; }
        public AIOperationSettingsDto? Structuring { get; set; }
        public AIOperationSettingsDto? Summarization { get; set; }
        public AIOperationSettingsDto? GlobalChat { get; set; }
        public AIOperationSettingsDto? NoteChat { get; set; }
    }
}
