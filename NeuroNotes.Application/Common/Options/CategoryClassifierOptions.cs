
namespace NeuroNotes.Application.Common.Options
{
    public class CategoryClassifierOptions
    {
        public const string SectionName = "CategoryClassifier";

        public string ModelPath { get; set; } = string.Empty;
        public string VocabPath { get; set; } = string.Empty;
        public float MinConfidenceThreshold { get; set; } 
        public int MaxSequenceLength { get; set; } 
    }
}
