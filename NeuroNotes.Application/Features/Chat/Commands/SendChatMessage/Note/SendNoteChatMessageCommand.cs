
using MediatR;

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Note
{
    public record SendNoteChatMessageCommand(Guid NoteId, string Message)
        : IRequest<SendChatMessageResponse>;
}
