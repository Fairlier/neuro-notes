using MediatR;
using NeuroNotes.Application.Features.Auth.Commands.LoginUser;
using System.Text.Json.Serialization;

namespace NeuroNotes.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<LoginUserResponse>
    {
        public string Token { get; set; } = string.Empty;

        [JsonIgnore]
        public string IpAddress { get; set; } = string.Empty;
    }
}
