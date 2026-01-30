
using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.StructureNote
{
    public record StructureNoteCommand(Guid NoteId) : IRequest;
}
