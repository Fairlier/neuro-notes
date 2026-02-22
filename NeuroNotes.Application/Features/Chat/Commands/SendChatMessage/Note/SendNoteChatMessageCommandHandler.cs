
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

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Note
{
    public class SendNoteChatMessageCommandHandler
        : IRequestHandler<SendNoteChatMessageCommand, SendChatMessageResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IAIContextService _contextService;
        private readonly IPromptService _promptService;
        private readonly AIOptions _aiOptions;
        private readonly ILogger<SendNoteChatMessageCommandHandler> _logger;

        public SendNoteChatMessageCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAIProviderFactory aiFactory,
            IAIContextService contextService,
            IPromptService promptService,
            IOptions<AIOptions> aiOptions,
            ILogger<SendNoteChatMessageCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _aiFactory = aiFactory;
            _contextService = contextService;
            _promptService = promptService;
            _aiOptions = aiOptions.Value;
            _logger = logger;
        }

        public async Task<SendChatMessageResponse> Handle(
            SendNoteChatMessageCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in SendNoteChatMessage.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Processing note chat message for User {UserId}, Note {NoteId}",
                userId, request.NoteId);

            var note = await _context.Notes.AsNoTracking()
                .FirstOrDefaultAsync(
                    n => n.Id == request.NoteId && n.UserId == userId,
                    cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for User {UserId}.",
                    request.NoteId, userId);
                throw new NotFoundException(nameof(Note), request.NoteId);
            }

            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var chatProvider = userAIProfile?.NoteChatProvider ?? _aiOptions.DefaultNoteChatProvider;
            var temperature = _aiOptions.DefaultNoteChatTemperature;

            var providerSettings = userAIProfile?.GetProviderSettings(chatProvider.ToString())
                ?? new Dictionary<string, string>();
            providerSettings["Temperature"] = temperature.ToString(CultureInfo.InvariantCulture);

            if (providerSettings.TryGetValue("NoteChatModel", out var noteModel) && !string.IsNullOrWhiteSpace(noteModel))
            {
                providerSettings["ChatModel"] = noteModel;
            }

            var context = _contextService.BuildContextFromNote(note);

            var systemPrompt = await _promptService.GetNoteChatSystemPromptAsync(userId);
            var fullSystemPrompt = string.IsNullOrWhiteSpace(context)
                ? systemPrompt
                : $"{systemPrompt}\n\n{context}";

            var session = await GetOrCreateNoteSessionAsync(
                userId, request.NoteId, note.Title, cancellationToken);

            var optimizedHistory = _contextService.OptimizeHistory(session.Messages);

            var chatService = _aiFactory.GetNoteChatService(chatProvider);
            var chatResponse = await chatService.SendMessageAsync(
                request.Message,
                optimizedHistory,
                fullSystemPrompt,
                providerSettings,
                cancellationToken);

            session.AddMessage(ChatRole.User, request.Message);
            session.AddMessage(ChatRole.Assistant, chatResponse);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note chat message processed. Provider: {Provider}, SessionId: {SessionId}, NoteId: {NoteId}",
                chatProvider, session.Id, request.NoteId);

            return new SendChatMessageResponse
            {
                Response = chatResponse,
                SessionId = session.Id
            };
        }

        private async Task<ChatSession> GetOrCreateNoteSessionAsync(
            string userId, Guid noteId, string noteTitle, CancellationToken token)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(
                    s => s.UserId == userId && s.RelatedNoteId == noteId,
                    token);

            if (session is null)
            {
                session = new ChatSession(userId, noteId, $"Note Chat: {noteTitle}");
                await _context.ChatSessions.AddAsync(session, token);
            }

            return session;
        }
    }
}
