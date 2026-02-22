using MediatR;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Note
{
    public record ClearNoteChatHistoryCommand(Guid NoteId) : IRequest;
}
