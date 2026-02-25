
using MediatR;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteSourceFile
{
    public record GetNoteSourceFileQuery(Guid NoteId) : IRequest<NoteSourceFileResponse>;
}
