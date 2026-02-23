using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.ProcessNote
{
    public record ProcessNoteCommand(Guid NoteId) : IRequest<Unit>;
}
