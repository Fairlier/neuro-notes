using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Enums;
using System.Text.Json;

namespace NeuroNotes.Application.Features.Users.Queries.GetUserAIProfile
{
    public class GetUserAIProfileQueryHandler : IRequestHandler<GetUserAIProfileQuery, UserAIProfileResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly AIOptions _aiDefaults;
        private readonly ILogger<GetUserAIProfileQueryHandler> _logger; 

        public GetUserAIProfileQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IOptions<AIOptions> aiDefaults,
            ILogger<GetUserAIProfileQueryHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _aiDefaults = aiDefaults.Value;
            _logger = logger;
        }

        public async Task<UserAIProfileResponse> Handle(GetUserAIProfileQuery request, CancellationToken token)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in GetUserAIProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts retrieving AI profile for User {UserId}.", 
                userId);

            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, token);

            if (userAIProfile is null)
            {
                _logger.LogInformation(
                    "AI Profile not found for User {UserId}. Returning default values.", 
                    userId);
            }

            var response = new UserAIProfileResponse
            {
                AIOperationLanguage = userAIProfile?.AIOperationLanguage ?? _aiDefaults.DefaultAIOperationLanguage,

                TranscriptionProvider = ResolveEnum(userAIProfile?.TranscriptionProvider, _aiDefaults.DefaultTranscriptionProvider),
                StructureProvider = ResolveEnum(userAIProfile?.StructureProvider, _aiDefaults.DefaultStructureProvider),
                SummaryProvider = ResolveEnum(userAIProfile?.SummaryProvider, _aiDefaults.DefaultSummaryProvider),
                ChatProvider = ResolveEnum(userAIProfile?.ChatProvider, _aiDefaults.DefaultChatProvider),

                CustomTranscriptionPrompt = userAIProfile?.CustomTranscriptionPrompt,
                CustomStructurePrompt = userAIProfile?.CustomStructurePrompt,
                CustomSummaryPrompt = userAIProfile?.CustomSummaryPrompt,
                CustomChatPrompt = userAIProfile?.CustomChatPrompt,

                ProviderSettings = new Dictionary<string, Dictionary<string, string>>()
            };

            var userProviderSettings = userAIProfile is not null
                ? DeserializeSettings(userAIProfile.ProviderSettingsJson)
                : new Dictionary<string, Dictionary<string, string>>();

            var allProviders = Enum.GetNames(typeof(TranscriptionProviderType))
                .Union(Enum.GetNames(typeof(ChatProviderType)))
                .Union(Enum.GetNames(typeof(StructureProviderType)))
                .Union(Enum.GetNames(typeof(SummaryProviderType)))
                .Distinct();

            foreach (var providerName in allProviders)
            {
                if (providerName.Contains("Local")) continue;

                var settingsForProvider = userProviderSettings.TryGetValue(providerName, out var existingDict)
                    ? new Dictionary<string, string>(existingDict) 
                    : new Dictionary<string, string>();

                if (!settingsForProvider.ContainsKey("ApiKey"))
                {
                    settingsForProvider["ApiKey"] = string.Empty;
                }

                response.ProviderSettings[providerName] = settingsForProvider;
            }

            _logger.LogInformation(
                "AI profile retrieved successfully for User {UserId}.", 
                userId);

            return response;
        }

        private T ResolveEnum<T>(T? databaseValue, T defaultValue) 
            where T : struct, Enum
        {
            if (databaseValue.HasValue && Convert.ToInt32(databaseValue.Value) != 0) return databaseValue.Value;

            return defaultValue;
        }

        private Dictionary<string, Dictionary<string, string>> DeserializeSettings(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, Dictionary<string, string>>();

            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                return result ?? new Dictionary<string, Dictionary<string, string>>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize ProviderSettingsJson for current user.");
                return new Dictionary<string, Dictionary<string, string>>();
            }
        }
    }
}
