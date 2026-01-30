
namespace NeuroNotes.Application.Common.Options
{
    public class GemmaLocalOptions
    {
        public string BaseUrl { get; set; } = string.Empty;

        public string EmbeddingModel { get; set; } = string.Empty;

        public int Dimensions { get; set; }

        public double MaxCosineDistance { get; set; } 

        public int SearchResultLimit { get; set; } 
    }
}
