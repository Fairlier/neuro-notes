using MediatR;

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Global
{
    public record SendGlobalChatMessageCommand(string Message) : IRequest<SendChatMessageResponse>;
}
