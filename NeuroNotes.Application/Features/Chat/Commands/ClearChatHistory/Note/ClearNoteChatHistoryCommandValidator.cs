using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Note
{
    public class ClearNoteChatHistoryCommandValidator : AbstractValidator<ClearNoteChatHistoryCommand>
    {
        public ClearNoteChatHistoryCommandValidator()
        {
            RuleFor(v => v.NoteId)
                .NotEmpty()
                .WithMessage("Note ID is required.");
        }
    }
}
