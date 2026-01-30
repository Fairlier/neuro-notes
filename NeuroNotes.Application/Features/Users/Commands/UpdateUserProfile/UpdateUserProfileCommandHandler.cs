using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly AppDefaultsOptions _defaults;
        private readonly ILogger<UpdateUserProfileCommandHandler> _logger; 

        public UpdateUserProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IOptions<AppDefaultsOptions> defaults,
            ILogger<UpdateUserProfileCommandHandler> logger) 
        {
            _context = context;
            _currentUser = currentUser;
            _defaults = defaults.Value;
            _logger = logger;
        }

        public async Task Handle(UpdateUserProfileCommand request, CancellationToken token)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in UpdateUserProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation("Starts updating user profile for User {UserId}.", userId);

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, token);

            if (userProfile is null)
            {
                _logger.LogInformation(
                    "UserProfile not found. Creating new profile for User {UserId}.", 
                    userId);

                var initLang = !string.IsNullOrWhiteSpace(request.InterfaceLanguage)
                    ? request.InterfaceLanguage
                    : _defaults.DefaultInterfaceLanguage;

                var initNick = !string.IsNullOrWhiteSpace(request.Nickname)
                    ? request.Nickname
                    : "User";

                userProfile = new UserProfile(userId, initNick, initLang);
                await _context.UserProfiles.AddAsync(userProfile, token);
            }
            else
            {
                var newNickname = !string.IsNullOrWhiteSpace(request.Nickname)
                    ? request.Nickname
                    : userProfile.Nickname;

                var newInterfaceLanguage = !string.IsNullOrWhiteSpace(request.InterfaceLanguage)
                    ? request.InterfaceLanguage
                    : userProfile.InterfaceLanguage;

                userProfile.Update(newNickname, newInterfaceLanguage);
            }

            await _context.SaveChangesAsync(token);

            _logger.LogInformation(
                "User profile updated successfully for User {UserId}.", 
                userId);
        }
    }
}
