
using FluentValidation;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Note
{
    public class GetNoteChatHistoryQueryValidator : AbstractValidator<GetNoteChatHistoryQuery>
    {
        public GetNoteChatHistoryQueryValidator()
        {
            RuleFor(x => x.NoteId)
                .NotEmpty().WithMessage("Note ID is required.");
        }
    }
}
