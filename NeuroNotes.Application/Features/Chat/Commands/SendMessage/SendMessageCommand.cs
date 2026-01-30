using MediatR;

namespace NeuroNotes.Application.Features.Chat.Commands.SendMessage
{
    public class SendMessageCommand : IRequest<SendMessageResponse>
    {
        public Guid? NoteId { get; set; } 
        public string Message { get; set; } = string.Empty;
    }
}
