using FileSignatures;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Infrastructure.Files.Formats;

namespace NeuroNotes.Infrastructure.Files.Validation
{
    public class FileSignatureValidator : IFileSignatureValidator
    {
        private readonly IFileFormatInspector _inspector;
        private readonly ILogger<FileSignatureValidator> _logger;

        public FileSignatureValidator(
            IFileFormatInspector inspector,
            ILogger<FileSignatureValidator> logger)
        {
            _inspector = inspector;
            _logger = logger;
        }

        public Task<bool> ValidateAudioFileAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            return ValidateFileAsync<Audio>(fileStream, "Audio", cancellationToken);
        }

        public Task<bool> ValidateImageFileAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            return ValidateFileAsync<Image>(fileStream, "Image", cancellationToken);
        }

        private Task<bool> ValidateFileAsync<TFormat>(
            Stream fileStream,
            string expectedTypeName,
            CancellationToken cancellationToken) where TFormat : FileFormat
        {
            if (fileStream is null || fileStream.Length == 0)
            {
                _logger.LogWarning("File validation failed: Stream is null or empty.");
                return Task.FromResult(false);
            }

            long originalPosition = 0;
            if (fileStream.CanSeek)
            {
                originalPosition = fileStream.Position;
                fileStream.Position = 0;
            }

            try
            {
                var format = _inspector.DetermineFileFormat(fileStream);

                if (format is null)
                {
                    _logger.LogWarning("File validation failed: Could not determine file signature.");
                    return Task.FromResult(false);
                }

                if (format is TFormat)
                {
                    _logger.LogDebug("File signature verified. Detected {Type} format: {MediaType}",
                        expectedTypeName, format.MediaType);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("File validation failed: Expected {ExpectedType}, but found {MediaType} ({Extension}).",
                    expectedTypeName, format.MediaType, format.Extension);

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file signature validation.");
                return Task.FromResult(false);
            }
            finally
            {
                if (fileStream.CanSeek)
                {
                    fileStream.Position = originalPosition;
                }
            }
        }
    }
}
