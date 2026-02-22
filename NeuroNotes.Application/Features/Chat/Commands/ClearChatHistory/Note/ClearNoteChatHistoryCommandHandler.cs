using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Note
{
    public class ClearNoteChatHistoryCommandHandler : IRequestHandler<ClearNoteChatHistoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ClearNoteChatHistoryCommandHandler> _logger;

        public ClearNoteChatHistoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ClearNoteChatHistoryCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task Handle(ClearNoteChatHistoryCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in ClearChatHistory.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts clearing chat history for User {UserId}. Note: {NoteId}",
                userId, request.NoteId);

            var noteExists = await _context.Notes.AsNoTracking()
                .AnyAsync(n => n.Id == request.NoteId && n.UserId == userId, cancellationToken);

            if (!noteExists)
                throw new NotFoundException(nameof(Note), request.NoteId);

            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RelatedNoteId == request.NoteId, cancellationToken);

            if (session is null)
            {
                _logger.LogInformation(
                    "Chat session not found for User {UserId} and Note {NoteId}. Operation skipped.",
                    userId, request.NoteId);
                return;
            }

            var messagesCount = session.Messages.Count;

            if (messagesCount > 0)
            {
                _context.ChatMessages.RemoveRange(session.Messages);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Successfully cleared {Count} messages from chat session {SessionId}.",
                messagesCount, session.Id);
        }
    }
}
