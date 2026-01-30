using MediatR;
using System.Text.Json.Serialization;

namespace NeuroNotes.Application.Features.Auth.Commands.LoginUser
{
    public class LoginUserCommand : IRequest<LoginUserResponse>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        [JsonIgnore]
        public string IpAddress { get; set; } = string.Empty;
    }
}
