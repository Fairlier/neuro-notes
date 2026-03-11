using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly IApplicationDbContext _context;
        private readonly AIOptions _aiOptions;
        private readonly AppDefaultsOptions _appDefaults;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IIdentityService identityService,
            IApplicationDbContext context,
            IOptions<AIOptions> aiOptions,
            IOptions<AppDefaultsOptions> appDefaults,
            ILogger<RegisterUserCommandHandler> logger) 
        {
            _identityService = identityService;
            _context = context;
            _aiOptions = aiOptions.Value;
            _appDefaults = appDefaults.Value;
            _logger = logger;
        }

        public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starts registering new user with email {Email}", 
                request.Email);

            var result = await _identityService.CreateUserAsync(request.Email, request.Password);

            var nickname = request.Email.Split('@')[0];

            var userProfile = new UserProfile(
                result.Id,
                nickname,
                _appDefaults.DefaultInterfaceLanguage,
                _appDefaults.DefaultTheme
            );

            await _context.UserProfiles.AddAsync(userProfile, cancellationToken);

            var userAIProfile = new UserAIProfile(result.Id);

            userAIProfile.UpdatePreferences(
                _aiOptions.DefaultAIOperationLanguage,
                _aiOptions.DefaultTranscriptionProvider,
                _aiOptions.DefaultStructureProvider,
                _aiOptions.DefaultSummaryProvider,
                _aiOptions.DefaultGlobalChatProvider,
                _aiOptions.DefaultNoteChatProvider
            );

            var providerSettings = new Dictionary<string, Dictionary<string, string>>
            {
                ["Gemini"] = new Dictionary<string, string>
                {
                    { "BaseUrl", _aiOptions.Gemini.BaseUrl },
                    { "TranscriptionModel", _aiOptions.Gemini.TranscriptionModel },
                    { "StructureModel", _aiOptions.Gemini.StructureModel },
                    { "SummaryModel", _aiOptions.Gemini.SummaryModel },
                    { "GlobalChatModel", _aiOptions.Gemini.GlobalChatModel },
                    { "NoteChatModel", _aiOptions.Gemini.NoteChatModel }
                },
                ["Mistral"] = new Dictionary<string, string>
                {
                    { "BaseUrl", _aiOptions.Mistral.BaseUrl },
                    { "TranscriptionModel", _aiOptions.Mistral.TranscriptionModel },
                    { "StructureModel", _aiOptions.Mistral.StructureModel },
                    { "SummaryModel", _aiOptions.Mistral.SummaryModel },
                    { "GlobalChatModel", _aiOptions.Mistral.GlobalChatModel },
                    { "NoteChatModel", _aiOptions.Mistral.NoteChatModel }
                }
            };

            foreach (var provider in providerSettings)
                userAIProfile.UpdateProviderSettings(provider.Key, provider.Value);

            await _context.UserAIProfiles.AddAsync(userAIProfile, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User registered successfully. UserId: {UserId}", 
                result.Id);

            return result;
        }
    }
}
