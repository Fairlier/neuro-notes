namespace NeuroNotes.Application.Interfaces.Files
{
    public interface IMimeTypeDetector
    {
        string GetMimeType(Stream stream, string fileName);
    }
}
