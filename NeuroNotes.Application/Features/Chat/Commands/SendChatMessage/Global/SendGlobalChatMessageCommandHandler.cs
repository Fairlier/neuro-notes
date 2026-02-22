using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Context;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;

namespace NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Global
{
    public class SendGlobalChatMessageCommandHandler : IRequestHandler<SendGlobalChatMessageCommand, SendChatMessageResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IAIContextService _contextService;
        private readonly IRagService _ragService;
        private readonly IPromptService _promptService;
        private readonly AIOptions _aiOptions; 
        private readonly ILogger<SendGlobalChatMessageCommandHandler> _logger;

        public SendGlobalChatMessageCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAIProviderFactory aiFactory,
            IAIContextService contextService,
            IRagService ragService,
            IPromptService promptService,
            IOptions<AIOptions> aiOptions, 
            ILogger<SendGlobalChatMessageCommandHandler> logger)
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

        public async Task<SendChatMessageResponse> Handle(SendGlobalChatMessageCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in SendMessage.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts processing chat message for User {UserId}", 
                userId);


            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var chatProvider = userAIProfile?.GlobalChatProvider ?? _aiOptions.DefaultGlobalChatProvider;

            var providerSettings = userAIProfile?.GetProviderSettings(chatProvider.ToString()) 
                ?? new Dictionary<string, string>();

            providerSettings["Temperature"] = _aiOptions.DefaultGlobalChatTemperature.ToString(CultureInfo.InvariantCulture);

            if (providerSettings.TryGetValue("GlobalChatModel", out var globalModel) && !string.IsNullOrWhiteSpace(globalModel))
            {
                providerSettings["ChatModel"] = globalModel;
            }

            var context = await _ragService.GetRelevantContextAsync(userId, request.Message, cancellationToken);

            var systemPrompt = await _promptService.GetGlobalChatSystemPromptAsync(userId);

            var fullSystemPrompt = string.IsNullOrWhiteSpace(context)
                ? systemPrompt
                : $"{systemPrompt}\n\n{context}";

            var session = await GetOrCreateGlobalSessionAsync(userId, cancellationToken);

            var optimizedHistory = _contextService.OptimizeHistory(session.Messages);

            var chatService = _aiFactory.GetGlobalChatService(chatProvider);

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
                "Global chat message processed successfully using provider {Provider}. SessionId: {SessionId}",
                chatProvider, session.Id);

            return new SendChatMessageResponse { Response = chatResponse, SessionId = session.Id };
        }

        private async Task<ChatSession> GetOrCreateGlobalSessionAsync(string userId, CancellationToken token)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RelatedNoteId == null, token);

            if (session is null)
            {
                session = new ChatSession(userId, null, "Global Chat");
                await _context.ChatSessions.AddAsync(session, token);
            }

            return session;
        }
    }
}
