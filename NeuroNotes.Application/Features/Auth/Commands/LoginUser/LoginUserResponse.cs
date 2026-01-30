
using System.Text.Json.Serialization;

namespace NeuroNotes.Application.Features.Auth.Commands.LoginUser
{
    public class LoginUserResponse
    {
        public string Id { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; set; }
    }
}
