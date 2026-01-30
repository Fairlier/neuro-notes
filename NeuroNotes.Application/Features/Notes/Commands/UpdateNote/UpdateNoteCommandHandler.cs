using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateNoteCommandHandler> _logger; 

        public UpdateNoteCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UpdateNoteCommandHandler> logger) 
        {
            _context = context;
            _currentUserService = currentUserService;
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

            if (request.Title != null)
            {
                entity.UpdateTitle(request.Title);
            }

            if (request.RawText != null)
            {
                entity.UpdateRawText(request.RawText);
            }

            if (request.StructuredText != null)
            {
                entity.UpdateStructuredText(request.StructuredText);
            }

            if (request.SummaryText != null)
            {
                entity.UpdateSummaryText(request.SummaryText);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note {NoteId} updated successfully.", 
                request.Id);
        }
    }
}
