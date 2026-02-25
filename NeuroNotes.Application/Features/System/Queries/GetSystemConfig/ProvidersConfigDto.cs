
namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public class ProvidersConfigDto
    {
        public List<string> Transcription { get; set; } = new();
        public List<string> Chat { get; set; } = new();
        public List<string> Structure { get; set; } = new();
        public List<string> Summary { get; set; } = new();
    }
}
