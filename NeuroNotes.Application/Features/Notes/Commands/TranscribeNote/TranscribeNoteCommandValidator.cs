using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.TranscribeNote
{
    public class TranscribeNoteCommandValidator : AbstractValidator<TranscribeNoteCommand>
    {
        public TranscribeNoteCommandValidator()
        {
            RuleFor(v => v.NoteId)
                .NotEmpty().WithMessage("Note ID is required.");
        }
    }
}
