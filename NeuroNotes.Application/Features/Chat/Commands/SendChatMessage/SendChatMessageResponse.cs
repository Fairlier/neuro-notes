namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage
{
    public class SendChatMessageResponse
    {
        public string Response { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
    }
}
