using NeuroNotes.Domain.Common;

namespace NeuroNotes.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; private set; } = string.Empty;
        public DateTime Expires { get; private set; }
        public string UserId { get; private set; } = string.Empty;
        public string CreatedByIp { get; private set; } = string.Empty;

        public DateTime? Revoked { get; private set; }
        public string? RevokedByIp { get; private set; }
        public string? ReplacedByToken { get; private set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;

        protected RefreshToken() { }

        public RefreshToken(string token, DateTime expires, string userId, string createdByIp)
        {
            Token = token;
            Expires = expires;
            UserId = userId;
            CreatedByIp = createdByIp;
        }

        public void Revoke(string ipAddress, string? replacedByToken = null)
        {
            Revoked = DateTime.UtcNow;
            RevokedByIp = ipAddress;
            ReplacedByToken = replacedByToken;
        }
    }
}
