
namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class NoteListResponse
    {
        public IList<NoteListItemDto> Notes { get; set; } = new List<NoteListItemDto>();
    }
}
