using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Features.Auth.Commands.LoginUser;
using NeuroNotes.Application.Features.Auth.Commands.RefreshToken;
using NeuroNotes.Application.Features.Auth.Commands.RegisterUser;

namespace NeuroNotes.Api.Controllers
{
    [Produces("application/json")]
    public class AuthController : BaseController
    {
        private readonly JwtOptions _jwtOptions;

        public AuthController(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RegisterUserResponse>> Register([FromBody] RegisterUserCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Вход в систему (получение пары токенов)
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginUserResponse>> Login([FromBody] LoginUserCommand command)
        {
            command.IpAddress = GetIpAddress();

            var result = await Mediator.Send(command);

            SetRefreshTokenCookie(result.RefreshToken);

            result.RefreshToken = null;

            return Ok(result);
        }

        /// <summary>
        /// Обновление Access токена через Refresh токен
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginUserResponse>> Refresh([FromBody] RefreshTokenCommand? command)
        {
            var token = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token) && command != null)
            {
                token = command.Token;
            }

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Refresh Token is required." });
            }

            var refreshCommand = new RefreshTokenCommand
            {
                Token = token,
                IpAddress = GetIpAddress()
            };

            var result = await Mediator.Send(refreshCommand);

            SetRefreshTokenCookie(result.RefreshToken);

            result.RefreshToken = null;

            return Ok(result);
        }

        /// <summary>
        /// Выход (удаление куки)
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("refreshToken"); // Не отозвал токен из бд
            return NoContent();
        }

        private void SetRefreshTokenCookie(string? token)
        {
            if (string.IsNullOrEmpty(token)) return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, 
                Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                SameSite = SameSiteMode.None,
                Secure = true 
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0.0.0.0";
        }
    }
}
