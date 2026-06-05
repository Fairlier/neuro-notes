
namespace NeuroNotes.Domain.ValueObjects
{
    public record AIOperationSettings(string? TargetLanguage, string? CustomPrompt, bool UseCustomPrompt, bool? IsAutomatic)
    {
        public static AIOperationSettings Default(bool? isAutomatic = null) => new(null, null, false, isAutomatic);
    }
}
