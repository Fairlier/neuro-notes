using MediatR;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public record GetNoteListQuery : IRequest<NoteListResponse>;
}
