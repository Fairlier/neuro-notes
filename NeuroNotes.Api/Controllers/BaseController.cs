using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NeuroNotes.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        private IMediator? _mediator;

        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        internal string? UserId => !User.Identity!.IsAuthenticated
            ? null
            : User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
