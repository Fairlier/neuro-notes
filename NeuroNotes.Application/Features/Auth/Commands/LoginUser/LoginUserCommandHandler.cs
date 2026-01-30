using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;

namespace NeuroNotes.Application.Features.Auth.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginUserResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            IIdentityService identityService,
            ILogger<LoginUserCommandHandler> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        public async Task<LoginUserResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starts user login process. Email: {Email}, IP: {IpAddress}",
                request.Email, request.IpAddress);

            var result = await _identityService.LoginUserAsync(request.Email, request.Password, request.IpAddress);

            _logger.LogInformation(
                "User logged in successfully. UserId: {UserId}",
                result.Id);

            return result;
        }
    }
}
