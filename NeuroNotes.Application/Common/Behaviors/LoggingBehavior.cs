using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;
using System.Diagnostics;

namespace NeuroNotes.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        private static readonly HashSet<string> SensitiveRequests = new(StringComparer.OrdinalIgnoreCase)
        {
            "LoginUserCommand",
            "RegisterUserCommand",
            "ChangePasswordCommand",
            "RefreshTokenCommand"
        };

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId ?? "Anonymous";
            var timer = Stopwatch.StartNew();

            object payload = SensitiveRequests.Contains(requestName)
                ? "(Hidden content)"
                : request;

            _logger.LogInformation("Starts handling request {Name}. User: {UserId}. Payload: {@Payload}",
                requestName, userId, payload);

            var response = await next();

            timer.Stop();

            _logger.LogInformation("Completes handling request {Name}. Duration: {Elapsed}ms.",
                requestName, timer.ElapsedMilliseconds);

            return response;
        }
    }
}
