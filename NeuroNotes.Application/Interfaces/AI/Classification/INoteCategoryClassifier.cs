
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Interfaces.AI.Classification
{
    public interface INoteCategoryClassifier
    {
        Task<NoteCategory> ClassifyAsync(string text, CancellationToken cancellationToken = default);
        Task<(NoteCategory Category, float Confidence)> ClassifyWithConfidenceAsync(string text, CancellationToken cancellationToken = default);
    }
}
