using MediatR;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Global
{
    public record GetGlobalChatHistoryQuery : IRequest<ChatHistoryResponse>;
}
