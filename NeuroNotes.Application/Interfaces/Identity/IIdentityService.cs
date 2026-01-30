using NeuroNotes.Application.Features.Auth.Commands.LoginUser;
using NeuroNotes.Application.Features.Auth.Commands.RegisterUser;

namespace NeuroNotes.Application.Interfaces.Identity
{
    public interface IIdentityService
    {
        Task<RegisterUserResponse> CreateUserAsync(string email, string password);

        Task<LoginUserResponse> LoginUserAsync(string email, string password, string ipAddress);

        Task<LoginUserResponse> RefreshTokenAsync(string token, string ipAddress);
    }
}
