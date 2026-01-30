using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Context;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;

namespace NeuroNotes.Application.Features.Chat.Commands.SendMessage
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IAIContextService _contextService;
        private readonly IRagService _ragService;
        private readonly IPromptService _promptService;
        private readonly AIOptions _aiOptions; 
        private readonly ILogger<SendMessageCommandHandler> _logger;

        public SendMessageCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAIProviderFactory aiFactory,
            IAIContextService contextService,
            IRagService ragService,
            IPromptService promptService,
            IOptions<AIOptions> aiOptions, 
            ILogger<SendMessageCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _aiFactory = aiFactory;
            _contextService = contextService;
            _ragService = ragService;
            _promptService = promptService;
            _aiOptions = aiOptions.Value; 
            _logger = logger;
        }

        public async Task<SendMessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in SendMessage.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts processing chat message for User {UserId}. NoteContext: {NoteId}", 
                userId, request.NoteId);


            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var chatProvider = userAIProfile?.ChatProvider ?? _aiOptions.DefaultChatProvider;

            var providerSettings = userAIProfile?.GetProviderSettings(chatProvider.ToString()) 
                ?? new Dictionary<string, string>();

            providerSettings["Temperature"] = _aiOptions.DefaultChatTemperature.ToString(CultureInfo.InvariantCulture);

            string context;
            if (request.NoteId.HasValue)
            {
                var note = await _context.Notes.AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == request.NoteId && n.UserId == userId, cancellationToken);

                if (note is null)
                {
                    _logger.LogWarning("Failed to retrieve note {NoteId} for User {UserId}. Note not found.", request.NoteId, userId);
                    throw new NotFoundException(nameof(Note), request.NoteId.Value);
                }

                context = _contextService.BuildContextFromNote(note);
            }
            else
            {
                context = await _ragService.GetRelevantContextAsync(userId, request.Message, cancellationToken);
            }

            var systemPrompt = await _promptService.GetChatSystemPromptAsync(userId);

            var fullSystemPrompt = string.IsNullOrWhiteSpace(context)
                ? systemPrompt
                : $"{systemPrompt}\n\n{context}";

            var session = await GetOrCreateSessionAsync(userId, request.NoteId, cancellationToken);

            var optimizedHistory = _contextService.OptimizeHistory(session.Messages);

            var chatService = _aiFactory.GetChatService(chatProvider);

            var chatResponse = await chatService.SendMessageAsync(
                request.Message,
                optimizedHistory,
                fullSystemPrompt,
                providerSettings: providerSettings,
                cancellationToken);

            session.AddMessage(ChatRole.User, request.Message);
            session.AddMessage(ChatRole.Assistant, chatResponse);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Chat message processed successfully using provider {Provider}. SessionId: {SessionId}",
                chatProvider, session.Id);

            return new SendMessageResponse { Response = chatResponse, SessionId = session.Id };
        }

        private async Task<ChatSession> GetOrCreateSessionAsync(string userId, Guid? noteId, CancellationToken token)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RelatedNoteId == noteId, token);

            if (session is null)
            {
                string title = noteId.HasValue ? "Note Chat" : "Global Chat";
                session = new ChatSession(userId, noteId, title);
                await _context.ChatSessions.AddAsync(session, token);
            }

            return session;
        }
    }
}
