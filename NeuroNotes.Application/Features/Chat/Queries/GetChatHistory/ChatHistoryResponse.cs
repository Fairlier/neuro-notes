namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory
{
    public class ChatHistoryResponse
    {
        public Guid SessionId { get; set; }
        public Guid? RelatedNoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public IList<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }
}
