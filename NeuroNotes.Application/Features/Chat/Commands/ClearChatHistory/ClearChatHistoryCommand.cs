using MediatR;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory
{
    public record ClearChatHistoryCommand(Guid? NoteId) : IRequest;
}
