using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Domain.Entities
{
    public class ChatSession : BaseEntity
    {
        public string UserId { get; private set; } = string.Empty;

        public Guid? RelatedNoteId { get; private set; }

        public string Title { get; private set; } = string.Empty;

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

        protected ChatSession() { }

        public ChatSession(string userId, Guid? relatedNoteId, string title)
        {
            UserId = userId;
            RelatedNoteId = relatedNoteId;
            Title = title;
        }

        public void AddMessage(ChatRole role, string content)
        {
            _messages.Add(new ChatMessage(Id, role, content));
            UpdatedAt = DateTime.UtcNow;
        }

        public void ClearHistory()
        {
            _messages.Clear();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
