using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Infrastructure.BackgroundJobs
{
    public class NoteProcessingSweeperJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoteProcessingSweeperJob> _logger;
        private const int ProcessingTimeoutMinutes = 60; 

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
                        .Where(n => n.IsProcessing && n.UpdatedAt < timeoutThreshold)
                        .Select(n => n.Id)
                        .ToListAsync(cancellationToken);
                }

                if (stuckNoteIds.Count == 0)
                {
                    _logger.LogDebug("No stuck notes found.");
                    return;
                }

                _logger.LogInformation(
                    "Found {Count} stuck notes (IsProcessing=true for over {Minutes} min). Starting recovery...",
                    stuckNoteIds.Count,
                    ProcessingTimeoutMinutes);

                foreach (var noteId in stuckNoteIds)
                {
                    await ProcessSingleNoteSafeAsync(noteId, cancellationToken);
                }

                _logger.LogInformation(
                    "Recovery completed for {Count} stuck notes.",
                    stuckNoteIds.Count);
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

                if (note == null || !note.IsProcessing)
                {
                    _logger.LogDebug(
                        "Note {NoteId} is no longer processing. Skipping.",
                        noteId);
                    return;
                }

                note.FailProcessing("System timeout: Processing took too long.");

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Recovered stuck note {NoteId}. " +
                    "Status: {Status}, IsProcessing: {IsProcessing}, Error: timeout.",
                    noteId, note.Status, note.IsProcessing);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogInformation("Concurrency conflict for note {NoteId}. Skipping recovery.", 
                    noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover stuck note {NoteId}.", noteId);
            }
        }
    }
}
