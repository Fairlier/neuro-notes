using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Constants;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, UploadAvatarDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;
        private readonly IFileSignatureValidator _fileValidator;
        private readonly AppDefaultsOptions _defaults;
        private readonly ILogger<UploadAvatarCommandHandler> _logger;

        public UploadAvatarCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage,
            IFileSignatureValidator fileValidator,
            IOptions<AppDefaultsOptions> defaults,
            ILogger<UploadAvatarCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _fileValidator = fileValidator;
            _defaults = defaults.Value;
            _logger = logger;
        }

        public async Task<UploadAvatarDto> Handle(
            UploadAvatarCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation("Uploading avatar for User {UserId}", userId);

            if (!request.FileStream.CanSeek)
            {
                throw new InvalidOperationException("File stream must be seekable.");
            }

            request.FileStream.Position = 0;

            if (!await _fileValidator.ValidateImageFileAsync(request.FileStream, cancellationToken))
            {
                _logger.LogWarning("Image file signature validation failed for User {UserId}", userId);
                throw new ArgumentException(
                    $"Invalid image file. Supported formats: {FileConstants.GetSupportedImageExtensionsString()}");
            }

            request.FileStream.Position = 0;

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            string? oldAvatarUrl = userProfile?.AvatarUrl;

            var avatarUrl = await _fileStorage.UploadPublicFileAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                cancellationToken);

            _logger.LogInformation("Avatar uploaded. URL: {AvatarUrl} for User {UserId}", avatarUrl, userId);

            if (userProfile is null)
            {
                userProfile = new UserProfile(userId, "User", _defaults.DefaultInterfaceLanguage);
                userProfile.UpdateAvatar(avatarUrl);
                await _context.UserProfiles.AddAsync(userProfile, cancellationToken);
            }
            else
            {
                userProfile.UpdateAvatar(avatarUrl);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(oldAvatarUrl))
            {
                try
                {
                    await _fileStorage.DeletePublicFileAsync(oldAvatarUrl, cancellationToken);
                    _logger.LogInformation("Old avatar deleted for User {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old avatar {OldAvatarUrl}", oldAvatarUrl);
                }
            }

            return new UploadAvatarDto { AvatarUrl = avatarUrl };
        }
    }
}
