
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Files;
using System.Collections.Concurrent;

namespace NeuroNotes.Infrastructure.Files.Storage
{
    public class MinioFileStorageService : IFileStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioOptions _options;
        private readonly ILogger<MinioFileStorageService> _logger;

        private static readonly ConcurrentDictionary<string, bool> _checkedBuckets = new();
        private static readonly SemaphoreSlim _bucketLock = new(1, 1);

        public MinioFileStorageService(
            IOptions<MinioOptions> options,
            ILogger<MinioFileStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

            _minioClient = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.WithSSL)
                .Build();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken)
        {
            if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty", nameof(fileName));

            _logger.LogDebug("Starting file upload. Original Name: {FileName}, ContentType: {ContentType}", fileName, contentType);

            try
            {
                await EnsureBucketExistsAsync(cancellationToken);

                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                if (fileStream.CanSeek && fileStream.Position > 0)
                {
                    fileStream.Position = 0;
                }

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(uniqueFileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

                _logger.LogInformation("File uploaded successfully. Key: {Key}", uniqueFileName);

                return uniqueFileName;
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "MinIO error during upload for {FileName}", fileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during upload for {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> GetFileUrlAsync(string fileKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey)) throw new ArgumentException("File key cannot be empty", nameof(fileKey));

            _logger.LogDebug("Generating presigned URL for {FileKey}", fileKey);

            try
            {
                var args = new PresignedGetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(fileKey)
                    .WithExpiry(3600);

                var url = await _minioClient.PresignedGetObjectAsync(args);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL for {FileKey}", fileKey);
                throw;
            }
        }

        public async Task DownloadToStreamAsync(string fileKey, Stream destinationStream, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey)) throw new ArgumentException("File key cannot be empty", nameof(fileKey));
            if (destinationStream == null) throw new ArgumentNullException(nameof(destinationStream));

            _logger.LogDebug("Downloading file {FileKey} to stream", fileKey);

            try
            {
                var args = new GetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(fileKey)
                    .WithCallbackStream((minioStream) =>
                    {
                        minioStream.CopyTo(destinationStream);
                    });

                await _minioClient.GetObjectAsync(args, cancellationToken);

                _logger.LogInformation("File {FileKey} downloaded successfully", fileKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming file {FileKey} from MinIO", fileKey);
                throw;
            }
        }

        public async Task<long> GetFileSizeAsync(string fileKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey)) throw new ArgumentException("File key cannot be empty", nameof(fileKey));

            _logger.LogDebug("Getting file stats for {FileKey}", fileKey);

            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(fileKey);

                var stat = await _minioClient.StatObjectAsync(args, cancellationToken);
                return stat.Size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stats for {FileKey}", fileKey);
                throw;
            }
        }

        private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
        {
            if (_checkedBuckets.ContainsKey(_options.BucketName)) return;

            await _bucketLock.WaitAsync(cancellationToken);
            try
            {
                if (_checkedBuckets.ContainsKey(_options.BucketName)) return;

                _logger.LogDebug("Checking existence of bucket {BucketName}", _options.BucketName);

                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
                bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

                if (!found)
                {
                    _logger.LogInformation("Bucket {BucketName} not found. Creating...", _options.BucketName);
                    var makeBucketArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
                    await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
                    _logger.LogInformation("Bucket {BucketName} created successfully", _options.BucketName);
                }

                _checkedBuckets.TryAdd(_options.BucketName, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring bucket {BucketName} exists", _options.BucketName);
                throw;
            }
            finally
            {
                _bucketLock.Release();
            }
        }
    }
}
