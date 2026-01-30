using MediatR;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.AudioFile
{
    public class CreateNoteFromAudioFileCommand : IRequest<CreateNoteResponse>
    {
        public string Title { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
