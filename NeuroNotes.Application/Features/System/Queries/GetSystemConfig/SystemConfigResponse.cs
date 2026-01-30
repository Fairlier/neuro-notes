
namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public class SystemConfigResponse
    {
        public List<string> SupportedAudioExtensions { get; set; } = new();
        public List<string> TranscriptionProviders { get; set; } = new();
        public List<string> ChatProviders { get; set; } = new();
        public List<string> StructureProviders { get; set; } = new();
        public List<string> SummaryProviders { get; set; } = new();
    }
}
