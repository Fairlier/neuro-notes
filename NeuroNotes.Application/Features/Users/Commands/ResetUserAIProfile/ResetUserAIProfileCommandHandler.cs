using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Users.Commands.ResetUserAIProfile
{
    public class ResetUserAIProfileCommandHandler : IRequestHandler<ResetUserAIProfileCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly AIOptions _aiDefaults;
        private readonly ILogger<ResetUserAIProfileCommandHandler> _logger;

        public ResetUserAIProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IOptions<AIOptions> aiDefaults,
            ILogger<ResetUserAIProfileCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _aiDefaults = aiDefaults.Value;
            _logger = logger;
        }

        public async Task Handle(ResetUserAIProfileCommand request, CancellationToken token)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in ResetUserAIProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Resets AI profile to default settings for User {UserId}.",
                userId);

            var userAIProfile = await _context.UserAIProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, token);

            if (userAIProfile is null)
            {
                _logger.LogWarning("User AI profile not found for User {UserId} during reset.", userId);
                return; 
            }

            userAIProfile.ResetToDefaults(
                _aiDefaults.DefaultAIOperationLanguage,
                _aiDefaults.DefaultTranscriptionProvider,
                _aiDefaults.DefaultStructureProvider,
                _aiDefaults.DefaultSummaryProvider,
                _aiDefaults.DefaultGlobalChatProvider,
                _aiDefaults.DefaultNoteChatProvider
            );

            var providerSettings = new Dictionary<string, Dictionary<string, string>>
            {
                ["Gemini"] = new Dictionary<string, string>
                {
                    { "BaseUrl", _aiDefaults.Gemini.BaseUrl },
                    { "TranscriptionModel", _aiDefaults.Gemini.TranscriptionModel },
                    { "StructureModel", _aiDefaults.Gemini.StructureModel },
                    { "SummaryModel", _aiDefaults.Gemini.SummaryModel },
                    { "GlobalChatModel", _aiDefaults.Gemini.GlobalChatModel },
                    { "NoteChatModel", _aiDefaults.Gemini.NoteChatModel }
                },
                ["Mistral"] = new Dictionary<string, string>
                {
                    { "BaseUrl", _aiDefaults.Mistral.BaseUrl },
                    { "TranscriptionModel", _aiDefaults.Mistral.TranscriptionModel },
                    { "StructureModel", _aiDefaults.Mistral.StructureModel },
                    { "SummaryModel", _aiDefaults.Mistral.SummaryModel },
                    { "GlobalChatModel", _aiDefaults.Mistral.GlobalChatModel },
                    { "NoteChatModel", _aiDefaults.Mistral.NoteChatModel }
                }
            };

            foreach (var provider in providerSettings)
            {
                userAIProfile.UpdateProviderSettings(provider.Key, provider.Value);
            }

            await _context.SaveChangesAsync(token);

            _logger.LogInformation(
                "AI profile successfully reset for User {UserId}.",
                userId);
        }
    }
}
