
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

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty", nameof(fileName));

            _logger.LogDebug("Starting private file upload. Name: {FileName}, ContentType: {ContentType}", fileName, contentType);

            try
            {
                await EnsureBucketExistsAsync(_options.PrivateBucketName, isPublic: false, cancellationToken);

                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                if (fileStream.CanSeek && fileStream.Position > 0)
                {
                    fileStream.Position = 0;
                }

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.PrivateBucketName)
                    .WithObject(uniqueFileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

                _logger.LogInformation("Private file uploaded. Key: {Key}", uniqueFileName);

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading private file {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> GetFileUrlAsync(string fileKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                throw new ArgumentException("File key cannot be empty", nameof(fileKey));

            _logger.LogDebug("Generating presigned URL for {FileKey}", fileKey);

            try
            {
                var args = new PresignedGetObjectArgs()
                    .WithBucket(_options.PrivateBucketName)
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

        public async Task DownloadToStreamAsync(
            string fileKey,
            Stream destinationStream,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                throw new ArgumentException("File key cannot be empty", nameof(fileKey));
            if (destinationStream == null)
                throw new ArgumentNullException(nameof(destinationStream));

            _logger.LogDebug("Downloading file {FileKey} to stream", fileKey);

            try
            {
                var tcs = new TaskCompletionSource<bool>();

                var args = new GetObjectArgs()
                    .WithBucket(_options.PrivateBucketName)
                    .WithObject(fileKey)
                    .WithCallbackStream(async (minioStream, ct) =>
                    {
                        try
                        {
                            await minioStream.CopyToAsync(destinationStream, ct);
                            tcs.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    });

                await _minioClient.GetObjectAsync(args, cancellationToken);
                await tcs.Task;

                _logger.LogInformation("File {FileKey} downloaded. Size: {Size} bytes", fileKey, destinationStream.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileKey}", fileKey);
                throw;
            }
        }

        public async Task<long> GetFileSizeAsync(string fileKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                throw new ArgumentException("File key cannot be empty", nameof(fileKey));

            _logger.LogDebug("Getting file stats for {FileKey}", fileKey);

            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(_options.PrivateBucketName)
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

        public async Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                throw new ArgumentException("File key cannot be empty", nameof(fileKey));

            _logger.LogDebug("Deleting private file {FileKey}", fileKey);

            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(_options.PrivateBucketName)
                    .WithObject(fileKey);

                await _minioClient.RemoveObjectAsync(args, cancellationToken);

                _logger.LogInformation("Private file {FileKey} deleted", fileKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting private file {FileKey}", fileKey);
                throw;
            }
        }

        public async Task<IReadOnlyList<string>> ListFileKeysAsync(
            string? prefix = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing files in bucket {BucketName} with prefix '{Prefix}'",
                _options.PrivateBucketName, prefix ?? "(none)");

            var fileKeys = new List<string>();

            try
            {
                var args = new ListObjectsArgs()
                    .WithBucket(_options.PrivateBucketName)
                    .WithRecursive(true);

                if (!string.IsNullOrEmpty(prefix))
                {
                    args = args.WithPrefix(prefix);
                }

                await foreach (var item in _minioClient.ListObjectsEnumAsync(args, cancellationToken))
                {
                    if (!item.IsDir)
                    {
                        fileKeys.Add(item.Key);
                    }
                }

                _logger.LogInformation("Listed {Count} files in bucket {BucketName}",
                    fileKeys.Count, _options.PrivateBucketName);

                return fileKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in bucket {BucketName}", _options.PrivateBucketName);
                throw;
            }
        }

        public async Task<string> UploadPublicFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty", nameof(fileName));

            _logger.LogDebug("Starting public file upload. Name: {FileName}", fileName);

            try
            {
                await EnsureBucketExistsAsync(_options.PublicBucketName, isPublic: true, cancellationToken);

                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                if (fileStream.CanSeek && fileStream.Position > 0)
                {
                    fileStream.Position = 0;
                }

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.PublicBucketName)
                    .WithObject(uniqueFileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

                var publicUrl = BuildPublicUrl(uniqueFileName);

                _logger.LogInformation("Public file uploaded. URL: {Url}", publicUrl);

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading public file {FileName}", fileName);
                throw;
            }
        }

        public async Task DeletePublicFileAsync(string fileUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;

            var fileKey = ExtractKeyFromPublicUrl(fileUrl);
            if (string.IsNullOrEmpty(fileKey)) return;

            _logger.LogDebug("Deleting public file {FileKey}", fileKey);

            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(_options.PublicBucketName)
                    .WithObject(fileKey);

                await _minioClient.RemoveObjectAsync(args, cancellationToken);

                _logger.LogInformation("Public file {FileKey} deleted", fileKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting public file {FileKey}", fileKey);
            }
        }

        public async Task<IReadOnlyList<string>> ListPublicFileKeysAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing files in public bucket {BucketName}", _options.PublicBucketName);

            var fileKeys = new List<string>();

            try
            {
                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.PublicBucketName);
                if (!await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken))
                {
                    return fileKeys;
                }

                var args = new ListObjectsArgs()
                    .WithBucket(_options.PublicBucketName)
                    .WithRecursive(true);

                await foreach (var item in _minioClient.ListObjectsEnumAsync(args, cancellationToken))
                {
                    if (!item.IsDir)
                    {
                        fileKeys.Add(item.Key);
                    }
                }

                _logger.LogInformation("Listed {Count} files in public bucket", fileKeys.Count);

                return fileKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in public bucket {BucketName}", _options.PublicBucketName);
                throw;
            }
        }

        private string BuildPublicUrl(string fileKey)
        {
            if (!string.IsNullOrEmpty(_options.PublicBaseUrl))
            {
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{fileKey}";
            }

            var scheme = _options.WithSSL ? "https" : "http";
            return $"{scheme}://{_options.Endpoint}/{_options.PublicBucketName}/{fileKey}";
        }

        private string? ExtractKeyFromPublicUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Segments.LastOrDefault()?.TrimStart('/');
            }
            catch
            {
                return url.Contains('/') ? url.Split('/').LastOrDefault() : url;
            }
        }

        private async Task EnsureBucketExistsAsync(
            string bucketName,
            bool isPublic,
            CancellationToken cancellationToken)
        {
            if (_checkedBuckets.ContainsKey(bucketName)) return;

            await _bucketLock.WaitAsync(cancellationToken);
            try
            {
                if (_checkedBuckets.ContainsKey(bucketName)) return;

                _logger.LogDebug("Checking existence of bucket {BucketName}", bucketName);

                var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
                bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

                if (!found)
                {
                    _logger.LogInformation("Bucket {BucketName} not found. Creating...", bucketName);

                    var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                    await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

                    if (isPublic)
                    {
                        await SetPublicReadPolicyAsync(bucketName, cancellationToken);
                    }

                    _logger.LogInformation("Bucket {BucketName} created (Public: {IsPublic})", bucketName, isPublic);
                }

                _checkedBuckets.TryAdd(bucketName, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring bucket {BucketName} exists", bucketName);
                throw;
            }
            finally
            {
                _bucketLock.Release();
            }
        }

        private async Task SetPublicReadPolicyAsync(string bucketName, CancellationToken cancellationToken)
        {
            var policy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Effect": "Allow",
                        "Principal": {"AWS": ["*"]},
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                    }
                ]
            }
            """;

            var args = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policy);

            await _minioClient.SetPolicyAsync(args, cancellationToken);

            _logger.LogInformation("Public read policy set for bucket {BucketName}", bucketName);
        }
    }
}
