using NeuroNotes.Domain.Common;

namespace NeuroNotes.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public string UserId { get; private set; } = string.Empty;

        public string Nickname { get; private set; } = string.Empty;

        public string InterfaceLanguage { get; private set; } = string.Empty;

        protected UserProfile() { }

        public UserProfile(string userId, string nickname, string interfaceLanguage)
        {
            UserId = userId;
            Nickname = nickname;
            InterfaceLanguage = interfaceLanguage;
        }

        public void Update(string nickname, string interfaceLanguage)
        {
            Nickname = nickname;
            InterfaceLanguage = interfaceLanguage;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
