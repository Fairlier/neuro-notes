
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Note
{
    public class GetNoteChatHistoryQueryHandler
        : IRequestHandler<GetNoteChatHistoryQuery, ChatHistoryResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetNoteChatHistoryQueryHandler> _logger;

        public GetNoteChatHistoryQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<GetNoteChatHistoryQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChatHistoryResponse> Handle(
            GetNoteChatHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in GetNoteChatHistory.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Retrieving note chat history for User {UserId}, Note {NoteId}",
                userId, request.NoteId);

            var note = await _context.Notes.AsNoTracking()
                .FirstOrDefaultAsync(
                    n => n.Id == request.NoteId && n.UserId == userId,
                    cancellationToken);

            if (note is null)
                throw new NotFoundException(nameof(Note), request.NoteId);

            var session = await _context.ChatSessions
                .AsNoTracking()
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(
                    s => s.UserId == userId && s.RelatedNoteId == request.NoteId,
                    cancellationToken);

            if (session is null)
            {
                _logger.LogInformation(
                    "Note chat session not found for User {NoteId}. Returning empty history.",
                    request.NoteId);

                return new ChatHistoryResponse
                {
                    SessionId = Guid.Empty,
                    RelatedNoteId = request.NoteId,
                    Title = $"Note Chat: {note.Title}",
                    Messages = new List<ChatMessageDto>()
                };
            }

            var messageDtos = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => _mapper.Map<ChatMessageDto>(m))
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved note chat history. SessionId: {SessionId}, Messages: {Count}",
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
