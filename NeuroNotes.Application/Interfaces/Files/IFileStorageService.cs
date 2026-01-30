namespace NeuroNotes.Application.Interfaces.Files
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, 
            CancellationToken cancellationToken);

        Task<string> GetFileUrlAsync(string fileKey, CancellationToken cancellationToken);

        Task DownloadToStreamAsync(string fileKey, Stream destinationStream, CancellationToken cancellationToken);

        Task<long> GetFileSizeAsync(string fileKey, CancellationToken cancellationToken);
    }
}
