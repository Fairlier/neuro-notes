using FluentValidation;
using NeuroNotes.Application.Common.Exceptions;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace NeuroNotes.Api.Middleware
{
    public class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

        public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            string result;

            switch (exception)
            {
                case ValidationException validationException:
                    code = HttpStatusCode.BadRequest;
                    _logger.LogWarning("Detects validation errors. {Message}", validationException.Message);
                    result = JsonSerializer.Serialize(new
                    {
                        Error = "Validation Failed",
                        Details = validationException.Errors.Select(e => new
                        {
                            Field = e.PropertyName,
                            Message = e.ErrorMessage
                        })
                    });
                    break;

                case NotFoundException:
                    code = HttpStatusCode.NotFound;
                    _logger.LogWarning("Fails to find resource. {Message}", exception.Message);
                    result = JsonSerializer.Serialize(new { Error = exception.Message });
                    break;

                case InvalidCredentialException:
                    code = HttpStatusCode.Unauthorized;
                    _logger.LogWarning("Rejects invalid credentials. {Message}", exception.Message);
                    result = JsonSerializer.Serialize(new { Error = exception.Message });
                    break;

                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    _logger.LogWarning("Denies unauthorized access. {Message}", exception.Message);
                    result = JsonSerializer.Serialize(new { Error = "Unauthorized access" });
                    break;

                default:
                    code = HttpStatusCode.InternalServerError;
                    _logger.LogError(exception, "Encounters unexpected error during request processing.");
                    result = JsonSerializer.Serialize(new
                    {
                        Error = exception.Message
                    });
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);
        }
    }
}
