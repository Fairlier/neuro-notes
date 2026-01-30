using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.TranscribeNote
{
    public record TranscribeNoteCommand(Guid NoteId) : IRequest;
}
