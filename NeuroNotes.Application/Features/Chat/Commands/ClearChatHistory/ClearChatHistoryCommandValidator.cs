using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory
{
    public class ClearChatHistoryCommandValidator : AbstractValidator<ClearChatHistoryCommand>
    {
        public ClearChatHistoryCommandValidator()
        {
            RuleFor(v => v.NoteId)
                .NotEmpty()
                .When(v => v.NoteId.HasValue)
                .WithMessage("NoteId cannot be an empty GUID.");
        }
    }
}
