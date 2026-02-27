using MediatR;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public record GetNoteListQuery : IRequest<NoteListResponse>
    {
        public NoteStatus? Status { get; init; }
        public NoteSourceType? SourceType { get; init; }
        public NoteCategory? Category { get; init; }
        public DateTime? CreatedFrom { get; init; }
        public DateTime? CreatedTo { get; init; }
        public DateTime? UpdatedFrom { get; init; }
        public DateTime? UpdatedTo { get; init; }

        public string? SearchTerm { get; init; }
        public SearchMode SearchMode { get; init; } = SearchMode.Title;

        public NoteSortBy SortBy { get; init; } = NoteSortBy.CreatedAt;
        public SortDirection SortDirection { get; init; } = SortDirection.Descending;

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
