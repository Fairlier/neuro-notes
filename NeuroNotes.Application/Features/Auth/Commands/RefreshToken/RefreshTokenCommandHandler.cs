using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Features.Auth.Commands.LoginUser;
using NeuroNotes.Application.Interfaces.Identity;

namespace NeuroNotes.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginUserResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IIdentityService identityService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        public async Task<LoginUserResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starts token refresh process. IP: {IpAddress}",
                request.IpAddress);

            var result = await _identityService.RefreshTokenAsync(request.Token, request.IpAddress);

            _logger.LogInformation(
                "Token refresh completed successfully for User {UserId}.",
                result.Id);

            return result;
        }
    }
}
