
namespace NeuroNotes.Application.Common.Options
{
    public class VoskLocalOptions
    {
        public string ModelsPath { get; set; } = string.Empty;

        public Dictionary<string, string> LanguageMap { get; set; } = new();
    }
}
