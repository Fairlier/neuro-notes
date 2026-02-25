using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Users.Commands.DeleteAvatar
{
    public class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeleteAvatarCommandHandler> _logger;

        public DeleteAvatarCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage,
            ILogger<DeleteAvatarCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation("Deleting avatar for User {UserId}", userId);

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (userProfile is null || string.IsNullOrEmpty(userProfile.AvatarUrl))
            {
                _logger.LogInformation("No avatar to delete for User {UserId}", userId);
                return;
            }

            var avatarUrl = userProfile.AvatarUrl;

            userProfile.UpdateAvatar(null);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _fileStorage.DeletePublicFileAsync(avatarUrl, cancellationToken);
                _logger.LogInformation("Avatar deleted for User {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete avatar file {AvatarUrl}", avatarUrl);
            }
        }
    }
}
