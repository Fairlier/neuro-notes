
namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class AIOperationSettingsDto
    {
        public string? TargetLanguage { get; set; }
        public string? CustomPrompt { get; set; }
        public bool? UseCustomPrompt { get; set; }
    }
}
