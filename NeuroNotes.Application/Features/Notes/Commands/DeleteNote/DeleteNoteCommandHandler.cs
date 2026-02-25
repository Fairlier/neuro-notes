
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeleteNoteCommandHandler> _logger;

        public DeleteNoteCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorage,
            ILogger<DeleteNoteCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileStorage = fileStorage;
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
                "Deleting note {NoteId} for User {UserId}.",
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

            var sourceFileKey = note.SourceFileUrl;

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Note {NoteId} deleted from database.", request.Id);

            if (!string.IsNullOrEmpty(sourceFileKey))
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(sourceFileKey, cancellationToken);

                    _logger.LogInformation(
                        "Source file {FileKey} deleted for Note {NoteId}.",
                        sourceFileKey, request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete source file {FileKey} for Note {NoteId}. File may require manual cleanup.",
                        sourceFileKey, request.Id);
                }
            }

            _logger.LogInformation("Note {NoteId} deleted successfully.", request.Id);
        }
    }
}
