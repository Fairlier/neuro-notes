using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteCommandValidator : AbstractValidator<DeleteNoteCommand>
    {
        public DeleteNoteCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty().WithMessage("Note ID is required.");
        }
    }
}
