using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;
using Pgvector;

namespace NeuroNotes.Domain.Entities
{
    public class NoteChunk : BaseEntity
    {
        public Guid NoteId { get; private set; }
        public Note? Note { get; private set; }

        public NoteChunkSourceType SourceType { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public Vector? Embedding { get; private set; }

        protected NoteChunk() { }

        public NoteChunk(Guid noteId, NoteChunkSourceType sourceType, 
            string content, Vector embedding)
        {
            NoteId = noteId;
            SourceType = sourceType;
            Content = content;
            Embedding = embedding;
        }
    }
}
