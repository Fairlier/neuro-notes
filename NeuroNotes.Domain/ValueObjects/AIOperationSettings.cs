
namespace NeuroNotes.Domain.ValueObjects
{
    public record AIOperationSettings(string? TargetLanguage, string? CustomPrompt, bool UseCustomPrompt)
    {
        public static AIOperationSettings Default() => new(null, null, false);
    }
}
