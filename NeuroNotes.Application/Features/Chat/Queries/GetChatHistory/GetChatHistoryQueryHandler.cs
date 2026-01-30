using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory
{
    public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, ChatHistoryResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetChatHistoryQueryHandler> _logger;

        public GetChatHistoryQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<GetChatHistoryQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChatHistoryResponse> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in GetChatHistory.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts retrieving chat history for User {UserId}. NoteContext: {NoteId}",
                userId, request.NoteId);

            var session = await _context.ChatSessions
                .AsNoTracking()
                .Include(s => s.Messages) 
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RelatedNoteId == request.NoteId, cancellationToken);

            if (session is null)
            {
                _logger.LogInformation(
                    "Chat session not found for User {UserId}. Returning empty history context.",
                    userId);

                return new ChatHistoryResponse
                {
                    SessionId = Guid.Empty,
                    RelatedNoteId = request.NoteId,
                    Title = request.NoteId.HasValue ? "Note Chat" : "Global Chat",
                    Messages = new List<ChatMessageDto>()
                };
            }

            var messageDtos = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => _mapper.Map<ChatMessageDto>(m))
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved chat history. SessionId: {SessionId}, Messages: {Count}",
                session.Id, messageDtos.Count);

            return new ChatHistoryResponse
            {
                SessionId = session.Id,
                RelatedNoteId = session.RelatedNoteId,
                Title = session.Title,
                Messages = messageDtos
            };
        }
    }
}
