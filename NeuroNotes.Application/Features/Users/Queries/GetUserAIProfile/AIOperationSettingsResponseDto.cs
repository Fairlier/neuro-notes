
namespace NeuroNotes.Application.Features.Users.Queries.GetUserAIProfile
{
    public class AIOperationSettingsResponseDto
    {
        public string? TargetLanguage { get; set; }
        public string? CustomPrompt { get; set; }
        public bool UseCustomPrompt { get; set; }
    }
}
