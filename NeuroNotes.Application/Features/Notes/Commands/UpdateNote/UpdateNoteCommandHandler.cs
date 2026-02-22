using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly ILogger<UpdateNoteCommandHandler> _logger;

        public UpdateNoteCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            INoteEmbeddingGenerator embeddingGenerator,
            ILogger<UpdateNoteCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _embeddingGenerator = embeddingGenerator;
            _logger = logger;
        }

        public async Task Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in UpdateNote.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts updating note {NoteId} for User {UserId}.",
                request.Id, userId);

            var entity = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);

            if (entity is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for User {UserId}. Update failed.", 
                    request.Id, userId);
                throw new NotFoundException(nameof(Note), request.Id);
            }

            var chunksToUpdate = new List<NoteChunkSourceType>();

            if (request.Title != null && entity.Title != request.Title)
            {
                entity.UpdateTitle(request.Title);
                chunksToUpdate.Add(NoteChunkSourceType.Title);
            }

            if (request.RawText != null && entity.RawText != request.RawText)
            {
                entity.UpdateRawText(request.RawText);
                chunksToUpdate.Add(NoteChunkSourceType.RawText);
            }

            if (request.StructuredText != null && entity.StructuredText != request.StructuredText)
            {
                entity.UpdateStructuredText(request.StructuredText);
                chunksToUpdate.Add(NoteChunkSourceType.StructuredText);
            }

            if (request.SummaryText != null && entity.SummaryText != request.SummaryText)
            {
                entity.UpdateSummaryText(request.SummaryText);
                chunksToUpdate.Add(NoteChunkSourceType.SummaryText);
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var sourceType in chunksToUpdate)
            {
                _logger.LogInformation(
                    "Updating embeddings for {SourceType} of Note {NoteId}.",
                    sourceType, request.Id);

                await _embeddingGenerator.UpdateEmbeddingsForSourceAsync(
                    entity, sourceType, cancellationToken);
            }

            _logger.LogInformation(
                "Note {NoteId} updated successfully.", 
                request.Id);
        }
    }
}
