
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.BackgroundJobs;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Notes.Commands.ProcessNote
{
    public class ProcessNoteCommandHandler : IRequestHandler<ProcessNoteCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly INoteCategoryClassifier _categoryClassifier;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<ProcessNoteCommandHandler> _logger;

        public ProcessNoteCommandHandler(
            IApplicationDbContext context,
            INoteCategoryClassifier categoryClassifier,
            INoteEmbeddingGenerator embeddingGenerator,
            IBackgroundJobService backgroundJobService,
            ILogger<ProcessNoteCommandHandler> logger)
        {
            _context = context;
            _categoryClassifier = categoryClassifier;
            _embeddingGenerator = embeddingGenerator;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public async Task<Unit> Handle(
            ProcessNoteCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting background processing for Note {NoteId}.",
                request.NoteId);

            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for processing.",
                    request.NoteId);
                return Unit.Value;
            }

            if (string.IsNullOrWhiteSpace(note.RawText))
            {
                _logger.LogWarning(
                    "Note {NoteId} has no RawText. Skipping processing and marking as finished.",
                    request.NoteId);

                note.FinishProcessing();
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }

            try
            {
                var userProfile = await _context.UserAIProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == note.UserId, cancellationToken);

                bool autoClassify = userProfile?.Classification.IsAutomatic ?? true; 

                if (autoClassify)
                {
                    _logger.LogInformation(
                        "Classifying category for Note {NoteId}...",
                        request.NoteId);

                    var (category, confidence) = await _categoryClassifier
                        .ClassifyWithConfidenceAsync(note.RawText, cancellationToken);

                    note.SetCategory(category);

                    _logger.LogInformation(
                        "Note {NoteId} classified as {Category} (confidence: {Confidence:F4}).",
                        request.NoteId, category, confidence);
                }
                else
                {
                    _logger.LogInformation(
                        "Auto-classification is disabled for User {UserId}. Skipping classification for Note {NoteId}.",
                        note.UserId, request.NoteId);
                }

                _logger.LogInformation(
                    "Generating embeddings for Note {NoteId}...",
                    request.NoteId);

                await _embeddingGenerator.GenerateAndSaveEmbeddingsAsync(note, cancellationToken);

                _logger.LogInformation(
                    "Embeddings generated for Note {NoteId}.",
                    request.NoteId);

                note.FinishProcessing();

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Note {NoteId} processed successfully. Status: {Status}",
                    request.NoteId, note.Status);

                if (userProfile?.Structuring.IsAutomatic == true)
                {
                    _logger.LogInformation(
                        "Auto-structuring enabled. Enqueuing structure job for Note {NoteId}.", 
                        request.NoteId);
                    _backgroundJobService.EnqueueStructureGeneration(note.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process Note {NoteId}.",
                    request.NoteId);

                note.FailProcessing($"Processing failed: {ex.Message}");
                await _context.SaveChangesAsync(CancellationToken.None);

                throw; 
            }

            return Unit.Value;
        }
    }
}
