
namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public class AudioConfigDto
    {
        public List<string> SupportedExtensions { get; set; } = new();
        public List<string> SupportedContentTypes { get; set; } = new();
        public long MaxUploadSizeBytes { get; set; }
        public string MaxUploadSizeFormatted { get; set; } = string.Empty;
    }
}
