using FileSignatures;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Files;

namespace NeuroNotes.Infrastructure.Files.Validation
{
    public class FileSignaturesMimeTypeDetector : IMimeTypeDetector
    {
        private readonly IFileFormatInspector _inspector;
        private readonly ILogger<FileSignaturesMimeTypeDetector> _logger;

        public FileSignaturesMimeTypeDetector(
            IFileFormatInspector inspector,
            ILogger<FileSignaturesMimeTypeDetector> logger)
        {
            _inspector = inspector;
            _logger = logger;
        }

        public string GetMimeType(Stream stream, string fileName)
        {
            if (stream is null)
            {
                _logger.LogError("Attempted to detect MIME type for null stream. File: {FileName}", fileName);
                throw new ArgumentNullException(nameof(stream));
            }

            _logger.LogDebug("Detecting MIME type for file: {FileName}", fileName);

            long originalPosition = 0;
            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                var format = _inspector.DetermineFileFormat(stream);

                if (format is null)
                {
                    _logger.LogWarning("Could not detect file signature for '{FileName}'. Defaulting to application/octet-stream.", fileName);
                    return "application/octet-stream";
                }

                _logger.LogDebug("Detected MIME type '{MimeType}' for file '{FileName}'.", format.MediaType, fileName);
                return format.MediaType;
            }
            finally
            {
                if (stream.CanSeek) stream.Position = originalPosition;
            }
        }
    }
}
