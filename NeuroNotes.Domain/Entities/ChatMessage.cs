using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Domain.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid ChatSessionId { get; private set; }

        // --- ДОБАВЛЕНО: Навигационное свойство ---
        // Это позволяет EF Core корректно обновлять связи в памяти
        public virtual ChatSession? ChatSession { get; private set; }

        public ChatRole Role { get; private set; }
        public string Content { get; private set; } = string.Empty;

        protected ChatMessage() { }

        public ChatMessage(Guid sessionId, ChatRole role, string content)
        {
            ChatSessionId = sessionId;
            Role = role;
            Content = content;
        }
    }
}
