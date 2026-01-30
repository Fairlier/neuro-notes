using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Infrastructure.BackgroundJobs
{
    public class NoteProcessingSweeperJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoteProcessingSweeperJob> _logger;
        private const int ProcessingTimeoutMinutes = 60; // TODO

        public NoteProcessingSweeperJob(
            IServiceScopeFactory scopeFactory,
            ILogger<NoteProcessingSweeperJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var timeoutThreshold = DateTime.UtcNow.AddMinutes(-ProcessingTimeoutMinutes);

            try
            {
                List<Guid> stuckNoteIds;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    stuckNoteIds = await context.Notes
                        .AsNoTracking() 
                        .Where(n => n.Status == NoteStatus.Processing && n.UpdatedAt < timeoutThreshold)
                        .Select(n => n.Id)
                        .ToListAsync(cancellationToken);
                }

                if (stuckNoteIds.Count == 0) return;

                _logger.LogInformation("Found {Count} stuck notes. Starting recovery...", stuckNoteIds.Count);

                foreach (var noteId in stuckNoteIds)
                {
                    await ProcessSingleNoteSafeAsync(noteId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in NoteProcessingSweeperJob execution.");
                throw;
            }
        }

        private async Task ProcessSingleNoteSafeAsync(Guid noteId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

                if (note == null || note.Status != NoteStatus.Processing)
                {
                    return;
                }

                note.FailProcessing("System timeout: Processing took too long.");

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Recovered stuck note {NoteId}. Status changed to Failed.", noteId);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogInformation("Concurrency conflict for note {NoteId}. Skipping recovery.", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover stuck note {NoteId}.", noteId);
            }
        }
    }
}
