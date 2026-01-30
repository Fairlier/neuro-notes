using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Interfaces.AI.Context
{
    public interface IAIContextService
    {
        IEnumerable<ChatMessage> OptimizeHistory(IEnumerable<ChatMessage> fullHistory, int maxTokens = 10000);
        string BuildContextFromNote(Note note);
        string BuildContextFromNotesList(IEnumerable<Note> notes);
    }
}
