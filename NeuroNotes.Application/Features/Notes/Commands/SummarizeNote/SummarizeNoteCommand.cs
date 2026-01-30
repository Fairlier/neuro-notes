using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.SummarizeNote
{
    public record SummarizeNoteCommand(Guid NoteId) : IRequest;
}
