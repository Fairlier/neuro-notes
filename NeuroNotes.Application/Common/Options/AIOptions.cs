using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Common.Options
{
    public class AIOptions
    {
        public const string SectionName = "AISettings";

        public string DefaultAIOperationLanguage { get; set; } = string.Empty;
        public TranscriptionProviderType DefaultTranscriptionProvider { get; set; }
        public StructureProviderType DefaultStructureProvider { get; set; }
        public SummaryProviderType DefaultSummaryProvider { get; set; }
        public ChatProviderType DefaultChatProvider { get; set; }

        public double DefaultChatTemperature { get; set; } 
        public double DefaultStructureTemperature { get; set; } 
        public double DefaultSummaryTemperature { get; set; } 

        public GeminiOptions Gemini { get; set; } = new();
        public VoskLocalOptions VoskLocal { get; set; } = new();
        public OllamaLocalOptions OllamaLocal { get; set; } = new();
        public MistralOptions Mistral { get; set; } = new();
    }
}
