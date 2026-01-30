using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.DeleteNote
{
    public record DeleteNoteCommand(Guid Id) : IRequest;
}
