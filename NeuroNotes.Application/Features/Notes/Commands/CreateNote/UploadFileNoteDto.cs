using Microsoft.AspNetCore.Http;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote
{
    public class UploadFileNoteDto
    {
        public string Title { get; set; } = string.Empty;

        public IFormFile File { get; set; } = null!;
    }
}
