
using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.DirectText
{
    public class CreateNoteFromDirectTextCommand : IRequest<CreateNoteResponse>
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
