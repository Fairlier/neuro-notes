using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.System.Queries.GetSystemConfig;

namespace NeuroNotes.Api.Controllers
{
    [Produces("application/json")]
    public class SystemController : BaseController
    {
        /// <summary>
        /// Получить конфигурацию сервиса (поддерживаемые форматы, доступные AI провайдеры)
        /// </summary>
        [HttpGet("config")]
        [ProducesResponseType(typeof(SystemConfigResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SystemConfigResponse>> GetConfig()
        {
            return Ok(await Mediator.Send(new GetSystemConfigQuery()));
        }
    }
}
