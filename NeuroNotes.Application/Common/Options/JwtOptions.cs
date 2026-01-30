
namespace NeuroNotes.Application.Common.Options
{
    public class JwtOptions
    {
        public const string SectionName = "JwtSettings";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        public string AccessTokenSecret { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; }

        public string RefreshTokenSecret { get; set; } = string.Empty;
        public int RefreshTokenExpiryDays { get; set; } 
    }
}
