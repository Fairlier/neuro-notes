
using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;

namespace NeuroNotes.Application.Features.Auth.Commands.LogoutUser
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(
            IIdentityService identityService,
            ILogger<LogoutCommandHandler> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User logout initiated. IP: {IpAddress}", request.IpAddress);

            if (!string.IsNullOrEmpty(request.Token))
            {
                await _identityService.RevokeTokenAsync(request.Token, request.IpAddress);
            }

            return Unit.Value;
        }
    }
}
