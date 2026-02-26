using MediatR;
using System.Text.Json.Serialization;

namespace NeuroNotes.Application.Features.Auth.Commands.LogoutUser
{
    public class LogoutCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;

        [JsonIgnore]
        public string IpAddress { get; set; } = string.Empty;
    }
}
