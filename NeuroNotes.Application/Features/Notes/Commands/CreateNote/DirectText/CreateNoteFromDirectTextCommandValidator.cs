
using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.DirectText
{
    public class CreateNoteFromDirectTextCommandValidator : AbstractValidator<CreateNoteFromDirectTextCommand>
    {
        public CreateNoteFromDirectTextCommandValidator()
        {
            RuleFor(v => v.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(v => v.Content)
                .NotEmpty().WithMessage("Content cannot be empty.");
        }
    }
}
