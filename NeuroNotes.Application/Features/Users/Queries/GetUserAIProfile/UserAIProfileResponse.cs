using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Users.Queries.GetUserAIProfile
{
    public class UserAIProfileResponse
    {
        public string AIOperationLanguage { get; set; } = string.Empty;

        public TranscriptionProviderType TranscriptionProvider { get; set; }
        public StructureProviderType StructureProvider { get; set; }
        public SummaryProviderType SummaryProvider { get; set; }
        public ChatProviderType GlobalChatProvider { get; set; }
        public ChatProviderType NoteChatProvider { get; set; }

        public AIOperationSettingsResponseDto? Transcription { get; set; }
        public AIOperationSettingsResponseDto? Structuring { get; set; }
        public AIOperationSettingsResponseDto? Summarization { get; set; }
        public AIOperationSettingsResponseDto? GlobalChat { get; set; }
        public AIOperationSettingsResponseDto? NoteChat { get; set; }

        public Dictionary<string, Dictionary<string, string>> ProviderSettings { get; set; } = new();
    }
}

