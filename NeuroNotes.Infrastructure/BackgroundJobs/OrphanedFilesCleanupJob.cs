using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Infrastructure.BackgroundJobs
{
    public class OrphanedFilesCleanupJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrphanedFilesCleanupJob> _logger;

        public OrphanedFilesCleanupJob(
            IServiceScopeFactory scopeFactory,
            ILogger<OrphanedFilesCleanupJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting orphaned files cleanup job...");

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

                await CleanupPrivateBucketAsync(context, fileStorage, cancellationToken);
                await CleanupPublicBucketAsync(context, fileStorage, cancellationToken);

                _logger.LogInformation("Orphaned files cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in OrphanedFilesCleanupJob execution.");
                throw;
            }
        }

        private async Task CleanupPrivateBucketAsync(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Cleaning up private bucket...");

            var usedFileKeys = await context.Notes
                .AsNoTracking()
                .Where(n => n.SourceFileUrl != null)
                .Select(n => n.SourceFileUrl!)
                .ToListAsync(cancellationToken);

            var usedKeysSet = usedFileKeys.ToHashSet();

            _logger.LogDebug("Found {Count} private file keys in database.", usedKeysSet.Count);

            var storageFileKeys = await fileStorage.ListFileKeysAsync(cancellationToken: cancellationToken);

            _logger.LogDebug("Found {Count} files in private storage.", storageFileKeys.Count);

            var orphanedKeys = storageFileKeys
                .Where(key => !usedKeysSet.Contains(key))
                .ToList();

            await DeleteOrphanedFilesAsync(
                orphanedKeys,
                key => fileStorage.DeleteFileAsync(key, cancellationToken),
                "private",
                cancellationToken);
        }

        private async Task CleanupPublicBucketAsync(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Cleaning up public bucket...");

            var usedAvatarUrls = await context.UserProfiles
                .AsNoTracking()
                .Where(p => p.AvatarUrl != null)
                .Select(p => p.AvatarUrl!)
                .ToListAsync(cancellationToken);

            var usedKeysSet = usedAvatarUrls
                .Select(ExtractKeyFromUrl)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToHashSet();

            _logger.LogDebug("Found {Count} public file keys in database.", usedKeysSet.Count);

            var storageFileKeys = await fileStorage.ListPublicFileKeysAsync(cancellationToken);

            _logger.LogDebug("Found {Count} files in public storage.", storageFileKeys.Count);

            var orphanedKeys = storageFileKeys
                .Where(key => !usedKeysSet.Contains(key))
                .ToList();

            await DeleteOrphanedFilesAsync(
                orphanedKeys,
                key => fileStorage.DeletePublicFileAsync(key, cancellationToken),
                "public",
                cancellationToken);
        }

        private async Task DeleteOrphanedFilesAsync(
            List<string> orphanedKeys,
            Func<string, Task> deleteFunc,
            string bucketType,
            CancellationToken cancellationToken)
        {
            if (orphanedKeys.Count == 0)
            {
                _logger.LogInformation("No orphaned files found in {BucketType} bucket.", bucketType);
                return;
            }

            _logger.LogWarning("Found {Count} orphaned files in {BucketType} bucket. Deleting...",
                orphanedKeys.Count, bucketType);

            var deletedCount = 0;
            var failedCount = 0;

            foreach (var fileKey in orphanedKeys)
            {
                try
                {
                    await deleteFunc(fileKey);
                    deletedCount++;
                    _logger.LogDebug("Deleted orphaned {BucketType} file: {FileKey}", bucketType, fileKey);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex, "Failed to delete orphaned {BucketType} file: {FileKey}",
                        bucketType, fileKey);
                }
            }

            _logger.LogInformation("{BucketType} bucket cleanup: Deleted {Deleted}, Failed {Failed}",
                bucketType, deletedCount, failedCount);
        }

        private static string? ExtractKeyFromUrl(string url)
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
    }
}
