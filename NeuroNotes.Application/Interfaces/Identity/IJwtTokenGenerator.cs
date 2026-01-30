using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Interfaces.Identity
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(string userId, string email, string userName); 
        RefreshToken GenerateRefreshToken(string userId, string ipAddress);
    }
}
