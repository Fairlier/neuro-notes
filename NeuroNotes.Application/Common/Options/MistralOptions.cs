
namespace NeuroNotes.Application.Common.Options
{
    public class MistralOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public string StructureModel { get; set; } = string.Empty;
        public string SummaryModel { get; set; } = string.Empty;
        public string TranscriptionModel { get; set; } = string.Empty;
        public string GlobalChatModel { get; set; } = string.Empty;
        public string NoteChatModel { get; set; } = string.Empty;
    }
}
