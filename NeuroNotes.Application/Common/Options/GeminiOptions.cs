
namespace NeuroNotes.Application.Common.Options
{
    public class GeminiOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public string ChatModel { get; set; } = string.Empty;
        public string StructureModel { get; set; } = string.Empty;
        public string SummaryModel { get; set; } = string.Empty;
        public string TranscriptionModel { get; set; } = string.Empty;
    }
}
