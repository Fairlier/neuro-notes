using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Global
{
    public class SendGlobalChatMessageCommandValidator : AbstractValidator<SendGlobalChatMessageCommand>
    {
        public SendGlobalChatMessageCommandValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty or whitespace only.")
                .MaximumLength(5000).WithMessage("Message is too long. The limit is 5000 characters.");
        }
    }
}
