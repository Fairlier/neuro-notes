
namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteSourceFile
{
    public class NoteSourceFileResponse
    {
        public Stream Stream { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
        public string? FileName { get; set; }
    }
}
