using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.Users.Commands.DeleteAvatar;
using NeuroNotes.Application.Features.Users.Commands.ResetUserAIProfile;
using NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile;
using NeuroNotes.Application.Features.Users.Commands.UpdateUserProfile;
using NeuroNotes.Application.Features.Users.Commands.UploadAvatar;
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
        /// Загрузить или обновить аватар пользователя
        /// </summary>
        /// <param name="file">Изображение (JPEG, PNG, GIF, WebP, макс. 10MB)</param>
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadAvatarDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UploadAvatarDto>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            using var stream = file.OpenReadStream();

            var command = new UploadAvatarCommand
            {
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Удалить аватар пользователя
        /// </summary>
        [HttpDelete("avatar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAvatar()
        {
            await Mediator.Send(new DeleteAvatarCommand());
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

        /// <summary>
        /// Сбросить настройки ИИ профиля до заводских по умолчанию
        /// </summary>
        [HttpPost("ai-profile/reset")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetAiProfile()
        {
            await Mediator.Send(new ResetUserAIProfileCommand());
            return NoContent();
        }
    }
}
