using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommandHandler : IRequestHandler<UpdateUserAIProfileCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly AIOptions _aiDefaults;
        private readonly ILogger<UpdateUserAIProfileCommandHandler> _logger; 

        public UpdateUserAIProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IOptions<AIOptions> aiDefaults,
            ILogger<UpdateUserAIProfileCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _aiDefaults = aiDefaults.Value;
            _logger = logger;
        }

        public async Task Handle(UpdateUserAIProfileCommand request, CancellationToken token)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in UpdateUserAIProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts updating AI profile for User {UserId}.", 
                userId);

            var userAIProfile = await _context.UserAIProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, token);

            if (userAIProfile is null)
            {
                _logger.LogInformation(
                    "User AI profile not found. Creating new profile for User {UserId}.", 
                    userId);
                userAIProfile = new UserAIProfile(userId);
                await _context.UserAIProfiles.AddAsync(userAIProfile, token);
            }

            var targetLanguage = !string.IsNullOrEmpty(request.AIOperationLanguage)
                ? request.AIOperationLanguage
                : (string.IsNullOrEmpty(userAIProfile.AIOperationLanguage) 
                    ? _aiDefaults.DefaultAIOperationLanguage 
                    : userAIProfile.AIOperationLanguage);

            var transcriptionProvider = ResolveProvider(
                request.TranscriptionProvider,
                userAIProfile.TranscriptionProvider,
                _aiDefaults.DefaultTranscriptionProvider);

            var structureProvider = ResolveProvider(
                request.StructureProvider,
                userAIProfile.StructureProvider,
                _aiDefaults.DefaultStructureProvider);

            var summaryProvider = ResolveProvider(
                request.SummaryProvider,
                userAIProfile.SummaryProvider,
                _aiDefaults.DefaultSummaryProvider);

            var chatProvider = ResolveProvider(
                request.ChatProvider,
                userAIProfile.ChatProvider,
                _aiDefaults.DefaultChatProvider);

            userAIProfile.UpdatePreferences(
                targetLanguage,
                transcriptionProvider,
                chatProvider,
                structureProvider,
                summaryProvider
            );

            if (request.ProviderSettings is not null)
            {
                foreach (var kvp in request.ProviderSettings)
                {
                    userAIProfile.UpdateProviderSettings(kvp.Key, kvp.Value);
                }
            }

            userAIProfile.SetPrompts(
                request.CustomTranscriptionPrompt ?? userAIProfile.CustomTranscriptionPrompt,
                request.CustomStructurePrompt ?? userAIProfile.CustomStructurePrompt,
                request.CustomChatPrompt ?? userAIProfile.CustomChatPrompt,
                request.CustomSummaryPrompt ?? userAIProfile.CustomSummaryPrompt
            );

            await _context.SaveChangesAsync(token);

            _logger.LogInformation(
                "User AI profile updated successfully for User {UserId}.", 
                userId);
        }

        private T ResolveProvider<T>(T? requestValue, T databaseValue, T defaultValue) 
            where T : struct, Enum
        {
            if (requestValue.HasValue) return requestValue.Value;

            if (Convert.ToInt32(databaseValue) != 0) return databaseValue;

            return defaultValue;
        }
    }
}
