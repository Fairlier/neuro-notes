
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Global
{
    public class ClearGlobalChatHistoryCommandHandler : IRequestHandler<ClearGlobalChatHistoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ClearGlobalChatHistoryCommandHandler> _logger;

        public ClearGlobalChatHistoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ClearGlobalChatHistoryCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task Handle(ClearGlobalChatHistoryCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            _logger.LogInformation("Clearing global chat history for User {UserId}", userId);

            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(
                    s => s.UserId == userId && s.RelatedNoteId == null,
                    cancellationToken);

            if (session is null)
            {
                _logger.LogInformation("No global chat session found for User {UserId}.", userId);
                return;
            }

            var count = session.Messages.Count;
            if (count > 0)
            {
                _context.ChatMessages.RemoveRange(session.Messages);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Cleared {Count} messages from global chat session {SessionId}.",
                count, session.Id);
        }
    }
}
