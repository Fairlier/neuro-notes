using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.BackgroundJobs;
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
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<UpdateNoteCommandHandler> _logger;

        public UpdateNoteCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IBackgroundJobService backgroundJobService,
            ILogger<UpdateNoteCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _backgroundJobService = backgroundJobService;
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

            bool needsReprocessing = false;

            if (request.Title != null && entity.Title != request.Title)
            {
                entity.UpdateTitle(request.Title);
                needsReprocessing = true;
            }

            if (request.RawText != null && entity.RawText != request.RawText)
            {
                entity.UpdateRawText(request.RawText);
                needsReprocessing = true;
            }

            if (request.StructuredText != null && entity.StructuredText != request.StructuredText)
            {
                entity.UpdateStructuredText(request.StructuredText);
                needsReprocessing = true;
            }

            if (request.SummaryText != null && entity.SummaryText != request.SummaryText)
            {
                entity.UpdateSummaryText(request.SummaryText);
                needsReprocessing = true;
            }

            if (request.Category.HasValue)
            {
                entity.SetCategory(request.Category.Value);
                _logger.LogInformation(
                    "Note {NoteId} category manually set to {Category}.",
                    request.Id, request.Category.Value);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (needsReprocessing)
            {
                entity.StartProcessing();
                await _context.SaveChangesAsync(cancellationToken);

                _backgroundJobService.EnqueueNoteReprocessing(entity.Id);

                _logger.LogInformation(
                    "Note {NoteId} queued for reprocessing. IsProcessing: {IsProcessing}",
                    request.Id, entity.IsProcessing);
            }
            else
            {
                _logger.LogInformation(
                    "Note {NoteId} updated without reprocessing.",
                    request.Id);
            }
        }
    }
}
