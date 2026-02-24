
namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class NoteListResponse
    {
        public IList<NoteListItemDto> Notes { get; set; } = new List<NoteListItemDto>();

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}
