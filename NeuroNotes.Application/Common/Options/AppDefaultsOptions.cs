
namespace NeuroNotes.Application.Common.Options
{
    public class AppDefaultsOptions
    {
        public const string SectionName = "AppDefaults";

        public string DefaultInterfaceLanguage { get; set; } = string.Empty;
        public string DefaultNickname { get; set; } = string.Empty;
        public string DefaultTheme { get; set; } = string.Empty;
    }
}
