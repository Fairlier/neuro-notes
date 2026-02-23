using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommandValidator : AbstractValidator<UpdateNoteCommand>
    {
        public UpdateNoteCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty().WithMessage("Note ID is required.");

            RuleFor(command => command.Title)
                .NotEmpty().WithMessage("Title cannot be empty.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
                .When(c => c.Title != null);

            RuleFor(command => command.Category)
                .IsInEnum().WithMessage("Invalid category.")
                .When(c => c.Category.HasValue);
        }
    }
}
