using MediatR;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory
{
    public record GetChatHistoryQuery(Guid? NoteId) : IRequest<ChatHistoryResponse>;
}
