using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly AppDefaultsOptions _defaults;
        private readonly ILogger<GetUserProfileQueryHandler> _logger;

        public GetUserProfileQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IOptions<AppDefaultsOptions> defaults,
            ILogger<GetUserProfileQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _defaults = defaults.Value;
            _logger = logger;
        }

        public async Task<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in GetUserProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation("Retrieving user profile for User {UserId}.", userId);

            var entity = await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (entity is null)
            {
                _logger.LogInformation("User profile not found for User {UserId}. Returning default values.", userId);

                return new UserProfileResponse
                {
                    Nickname = _defaults.DefaultNickname,
                    InterfaceLanguage = _defaults.DefaultInterfaceLanguage,
                    Theme = _defaults.DefaultTheme,
                    AvatarUrl = null
                };
            }

            var language = !string.IsNullOrEmpty(entity.InterfaceLanguage)
                ? entity.InterfaceLanguage
                : _defaults.DefaultInterfaceLanguage;

            var nickname = !string.IsNullOrEmpty(entity.Nickname)
                ? entity.Nickname
                : _defaults.DefaultNickname;

            var theme = !string.IsNullOrEmpty(entity.Theme)
                ? entity.Theme
                : _defaults.DefaultTheme;

            _logger.LogInformation("User profile retrieved for User {UserId}.", userId);

            return new UserProfileResponse
            {
                Nickname = nickname,
                InterfaceLanguage = language,
                Theme = theme,
                AvatarUrl = entity.AvatarUrl
            };
        }
    }
}
