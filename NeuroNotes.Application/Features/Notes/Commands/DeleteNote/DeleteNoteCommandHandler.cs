
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteNoteCommandHandler> _logger; 

        public DeleteNoteCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DeleteNoteCommandHandler> logger) 
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in DeleteNote.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts deleting note {NoteId} for User {UserId}.",
                request.Id, userId);

            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for deletion. User: {UserId}", 
                    request.Id, userId);
                throw new NotFoundException(nameof(Note), request.Id);
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note {NoteId} deleted successfully.", 
                request.Id);
        }
    }
}
