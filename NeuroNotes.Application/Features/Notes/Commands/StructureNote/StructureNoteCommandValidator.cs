using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.StructureNote
{
    public class StructureNoteCommandValidator : AbstractValidator<StructureNoteCommand>
    {
        public StructureNoteCommandValidator()
        {
            RuleFor(v => v.NoteId)
                .NotEmpty().WithMessage("Note ID is required.");
        }
    }
}
