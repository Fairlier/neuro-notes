using FluentValidation;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommandValidator : AbstractValidator<UpdateUserAIProfileCommand>
    {
        public UpdateUserAIProfileCommandValidator()
        {
            RuleFor(v => v.AIOperationLanguage)
                .Length(2).WithMessage("Language code must be exactly 2 characters (e.g., 'en').")
                .When(v => v.AIOperationLanguage != null);

            RuleFor(v => v.TranscriptionProvider)
                .IsInEnum().WithMessage("Invalid transcription provider.")
                .When(v => v.TranscriptionProvider.HasValue);

            RuleFor(v => v.StructureProvider)
                .IsInEnum().WithMessage("Invalid structure provider.")
                .When(v => v.StructureProvider.HasValue);

            RuleFor(v => v.SummaryProvider)
                .IsInEnum().WithMessage("Invalid summary provider.")
                .When(v => v.SummaryProvider.HasValue);

            RuleFor(v => v.GlobalChatProvider)
                .IsInEnum().WithMessage("Invalid global chat provider.")
                .When(v => v.GlobalChatProvider.HasValue);

            RuleFor(v => v.NoteChatProvider)
                .IsInEnum().WithMessage("Invalid note chat provider.")
                .When(v => v.NoteChatProvider.HasValue);

            When(v => v.Transcription != null, () =>
            {
                RuleFor(v => v.Transcription!.TargetLanguage)
                    .Length(2).WithMessage("Language code must be exactly 2 characters.")
                    .When(v => v.Transcription!.TargetLanguage != null);

                RuleFor(v => v.Transcription!.CustomPrompt)
                    .MaximumLength(2000).WithMessage("Transcription prompt is too long (max 2000 chars).")
                    .When(v => v.Transcription!.CustomPrompt != null);
            });

            When(v => v.Structuring != null, () =>
            {
                RuleFor(v => v.Structuring!.TargetLanguage)
                    .Length(2).WithMessage("Language code must be exactly 2 characters.")
                    .When(v => v.Structuring!.TargetLanguage != null);

                RuleFor(v => v.Structuring!.CustomPrompt)
                    .MaximumLength(5000).WithMessage("Structuring prompt is too long (max 5000 chars).")
                    .When(v => v.Structuring!.CustomPrompt != null);
            });

            When(v => v.Summarization != null, () =>
            {
                RuleFor(v => v.Summarization!.TargetLanguage)
                    .Length(2).WithMessage("Language code must be exactly 2 characters.")
                    .When(v => v.Summarization!.TargetLanguage != null);

                RuleFor(v => v.Summarization!.CustomPrompt)
                    .MaximumLength(2000).WithMessage("Summarization prompt is too long (max 2000 chars).")
                    .When(v => v.Summarization!.CustomPrompt != null);
            });

            When(v => v.GlobalChat != null, () =>
            {
                RuleFor(v => v.GlobalChat!.TargetLanguage)
                    .Length(2).WithMessage("Language code must be exactly 2 characters.")
                    .When(v => v.GlobalChat!.TargetLanguage != null);

                RuleFor(v => v.GlobalChat!.CustomPrompt)
                    .MaximumLength(5000).WithMessage("Global chat prompt is too long (max 5000 chars).")
                    .When(v => v.GlobalChat!.CustomPrompt != null);
            });

            When(v => v.NoteChat != null, () =>
            {
                RuleFor(v => v.NoteChat!.TargetLanguage)
                    .Length(2).WithMessage("Language code must be exactly 2 characters.")
                    .When(v => v.NoteChat!.TargetLanguage != null);

                RuleFor(v => v.NoteChat!.CustomPrompt)
                    .MaximumLength(5000).WithMessage("Note chat prompt is too long (max 5000 chars).")
                    .When(v => v.NoteChat!.CustomPrompt != null);
            });
        }
    }
}
