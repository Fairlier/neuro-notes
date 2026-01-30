using FluentValidation;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommandValidator : AbstractValidator<UpdateUserAIProfileCommand>
    {
        public UpdateUserAIProfileCommandValidator()
        {
            RuleFor(v => v.AIOperationLanguage)
                .Length(2, 5).WithMessage("Language code must be between 2 and 5 characters (e.g., 'en', 'en-US').")
                .When(v => v.AIOperationLanguage != null);

            RuleFor(v => v.TranscriptionProvider)
                .IsInEnum().WithMessage("Invalid transcription provider.")
                .When(v => v.TranscriptionProvider.HasValue);

            RuleFor(v => v.StructureProvider)
                .IsInEnum().WithMessage("Invalid structure provider.")
                .When(v => v.StructureProvider.HasValue);

            RuleFor(v => v.ChatProvider)
                .IsInEnum().WithMessage("Invalid chat provider.")
                .When(v => v.ChatProvider.HasValue);

            RuleFor(v => v.SummaryProvider)
                .IsInEnum().WithMessage("Invalid summary provider.")
                .When(v => v.SummaryProvider.HasValue);

            RuleFor(v => v.CustomTranscriptionPrompt)
                .MaximumLength(2000).WithMessage("Transcription prompt is too long (max 2000 chars).")
                .When(v => v.CustomTranscriptionPrompt != null);

            RuleFor(v => v.CustomStructurePrompt)
                .MaximumLength(5000).WithMessage("Structure prompt is too long (max 5000 chars).")
                .When(v => v.CustomStructurePrompt != null);

            RuleFor(v => v.CustomChatPrompt)
                .MaximumLength(5000).WithMessage("Chat prompt is too long (max 5000 chars).")
                .When(v => v.CustomChatPrompt != null);

            RuleFor(v => v.CustomSummaryPrompt)
                .MaximumLength(2000).WithMessage("Summary prompt is too long (max 2000 chars).")
                .When(v => v.CustomSummaryPrompt != null);
        }
    }
}
