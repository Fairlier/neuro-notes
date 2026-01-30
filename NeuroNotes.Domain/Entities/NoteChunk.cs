using NeuroNotes.Domain.Common;
using Pgvector;

namespace NeuroNotes.Domain.Entities
{
    public class NoteChunk : BaseEntity
    {
        public Guid NoteId { get; private set; }
        public virtual Note? Note { get; private set; }

        public string Content { get; private set; } = string.Empty;

        // Вектор. Для Gemini размерность обычно 768.
        public Vector? Embedding { get; private set; }

        protected NoteChunk() { }

        public NoteChunk(Guid noteId, string content, Vector embedding)
        {
            NoteId = noteId;
            Content = content;
            Embedding = embedding;
        }
    }
}
