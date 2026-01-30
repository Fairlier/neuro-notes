namespace NeuroNotes.Application.Features.Chat.Commands.SendMessage
{
    public class SendMessageResponse
    {
        public string Response { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
    }
}
