
using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Note
{
    public class SendNoteChatMessageCommandValidator : AbstractValidator<SendNoteChatMessageCommand>
    {
        public SendNoteChatMessageCommandValidator()
        {
            RuleFor(x => x.NoteId)
                .NotEmpty().WithMessage("Note ID is required.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(5000).WithMessage("Message is too long. The limit is 5000 characters.");
        }
    }
}
