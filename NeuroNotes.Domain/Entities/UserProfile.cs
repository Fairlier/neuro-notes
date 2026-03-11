using NeuroNotes.Domain.Common;

namespace NeuroNotes.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public string UserId { get; private set; } = string.Empty;

        public string Nickname { get; private set; } = string.Empty;

        public string InterfaceLanguage { get; private set; } = string.Empty;
        public string Theme { get; private set; } = string.Empty;

        public string? AvatarUrl { get; private set; }

        protected UserProfile() { }

        public UserProfile(string userId, string nickname, string interfaceLanguage, string theme)
        {
            UserId = userId;
            Nickname = nickname;
            InterfaceLanguage = interfaceLanguage;
            Theme = theme;
        }

        public void Update(string nickname, string interfaceLanguage, string theme)
        {
            Nickname = nickname;
            InterfaceLanguage = interfaceLanguage;
            Theme = theme;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateNickname(string nickname)
        {
            Nickname = nickname;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateInterfaceLanguage(string language)
        {
            InterfaceLanguage = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateTheme(string theme)
        {
            Theme = theme;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
