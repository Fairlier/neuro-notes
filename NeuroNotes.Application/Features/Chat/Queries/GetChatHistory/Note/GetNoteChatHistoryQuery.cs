
using MediatR;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Note
{
    public record GetNoteChatHistoryQuery(Guid NoteId) : IRequest<ChatHistoryResponse>;
}
