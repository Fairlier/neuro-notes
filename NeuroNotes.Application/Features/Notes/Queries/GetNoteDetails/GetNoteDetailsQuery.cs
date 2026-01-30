using MediatR;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteDetails
{
    public record GetNoteDetailsQuery(Guid Id) : IRequest<NoteDetailsResponse>;
}
