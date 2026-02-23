
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Notes.Commands.ProcessNote
{
    public class ProcessNoteCommandHandler : IRequestHandler<ProcessNoteCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly INoteCategoryClassifier _categoryClassifier;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly ILogger<ProcessNoteCommandHandler> _logger;

        public ProcessNoteCommandHandler(
            IApplicationDbContext context,
            INoteCategoryClassifier categoryClassifier,
            INoteEmbeddingGenerator embeddingGenerator,
            ILogger<ProcessNoteCommandHandler> logger)
        {
            _context = context;
            _categoryClassifier = categoryClassifier;
            _embeddingGenerator = embeddingGenerator;
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
                    "Note {NoteId} has no RawText. Skipping processing.",
                    request.NoteId);
                return Unit.Value;
            }

            try
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
