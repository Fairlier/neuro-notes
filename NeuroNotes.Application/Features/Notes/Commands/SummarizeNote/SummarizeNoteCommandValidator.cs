using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.SummarizeNote
{
    public class SummarizeNoteCommandValidator : AbstractValidator<SummarizeNoteCommand>
    {
        public SummarizeNoteCommandValidator()
        {
            RuleFor(v => v.NoteId)
                .NotEmpty().WithMessage("Note ID is required.");
        }
    }
}
