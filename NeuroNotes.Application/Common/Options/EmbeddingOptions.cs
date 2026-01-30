using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Common.Options
{
    public class EmbeddingOptions
    {
        public const string SectionName = "EmbeddingSettings";

        public EmbeddingProviderType DefaultEmbeddingProvider { get; set; }

        public GemmaLocalOptions GemmaLocal { get; set; } = new();
    }
}
