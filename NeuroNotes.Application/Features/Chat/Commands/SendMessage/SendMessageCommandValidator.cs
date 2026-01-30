using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Commands.SendMessage
{
    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty or whitespace only.")
                .MaximumLength(5000).WithMessage("Message is too long. The limit is 5000 characters.");

            RuleFor(x => x.NoteId)
                .NotEmpty()
                .When(x => x.NoteId.HasValue)
                .WithMessage("Invalid Note ID.");
        }
    }
}
