namespace NeuroNotes.Application.Interfaces.Files
{
    public interface IFileSignatureValidator
    {
        Task<bool> ValidateAudioFileAsync(Stream fileStream, CancellationToken cancellationToken);
    }
}
