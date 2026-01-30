using MediatR;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommand : IRequest
    {
        public string? AIOperationLanguage { get; set; }

        public TranscriptionProviderType? TranscriptionProvider { get; set; }
        public string? CustomTranscriptionPrompt { get; set; }

        public StructureProviderType? StructureProvider { get; set; }
        public string? CustomStructurePrompt { get; set; }

        public SummaryProviderType? SummaryProvider { get; set; }
        public string? CustomSummaryPrompt { get; set; }

        public ChatProviderType? ChatProvider { get; set; }
        public string? CustomChatPrompt { get; set; }

        public Dictionary<string, Dictionary<string, string>>? ProviderSettings { get; set; }
    }
}
