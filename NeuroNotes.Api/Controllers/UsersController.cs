using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile;
using NeuroNotes.Application.Features.Users.Commands.UpdateUserProfile;
using NeuroNotes.Application.Features.Users.Queries.GetUserAIProfile;
using NeuroNotes.Application.Features.Users.Queries.GetUserProfile;

namespace NeuroNotes.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class UsersController : BaseController
    {
        /// <summary>
        /// Получить основной профиль (Никнейм, Язык интерфейса)
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            return Ok(await Mediator.Send(new GetUserProfileQuery()));
        }

        /// <summary>
        /// Обновить основной профиль (Никнейм, Язык интерфейса)
        /// </summary>
        [HttpPatch("profile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Получить настройки ИИ (Провайдеры, Ключи, Промпты)
        /// </summary>
        [HttpGet("ai-profile")]
        [ProducesResponseType(typeof(UserAIProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserAIProfileResponse>> GetAiProfile()
        {
            return Ok(await Mediator.Send(new GetUserAIProfileQuery()));
        }

        /// <summary>
        /// Обновить настройки ИИ (Частичное обновление)
        /// </summary>
        [HttpPatch("ai-profile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateAiProfile([FromBody] UpdateUserAIProfileCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
