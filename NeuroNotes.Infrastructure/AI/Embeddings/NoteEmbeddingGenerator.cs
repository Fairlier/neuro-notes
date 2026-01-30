using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Text;

namespace NeuroNotes.Infrastructure.AI.Embeddings
{
    public class NoteEmbeddingGenerator : INoteEmbeddingGenerator
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmbeddingModelFactory _embeddingFactory;
        private readonly ILogger<NoteEmbeddingGenerator> _logger;

        private readonly string[] _separators = { "\n\n", "\n", ". ", "? ", "! ", " ", "" };

        public NoteEmbeddingGenerator(
            IApplicationDbContext context,
            IEmbeddingModelFactory embeddingFactory,
            ILogger<NoteEmbeddingGenerator> logger)
        {
            _context = context;
            _embeddingFactory = embeddingFactory;
            _logger = logger;
        }

        public async Task GenerateAndSaveEmbeddingsAsync(Note note, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting embedding generation for Note {NoteId}.", 
                note.Id);

            try
            {
                var embeddingService = _embeddingFactory.Create();

                await _context.NoteChunks
                    .Where(c => c.NoteId == note.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                var textToIndex = !string.IsNullOrWhiteSpace(note.StructuredText)
                    ? note.StructuredText
                    : note.RawText;

                if (string.IsNullOrWhiteSpace(textToIndex))
                {
                    _logger.LogWarning(
                        "Skipping embedding generation: No text available for Note {NoteId}.", 
                        note.Id);
                    return;
                }

                var chunks = SplitTextRecursively(textToIndex, 1000, 200);

                _logger.LogInformation(
                    "Text split into {Count} chunks for Note {NoteId}.", 
                    chunks.Count, note.Id);

                if (chunks.Count == 0) return;

                var safeTitle = (note.Title ?? "Untitled").Replace("\n", " ").Trim();

                var payloadForEmbedding = chunks.Select(chunkText =>
                    $"Title: {safeTitle}\nContent: {chunkText}"
                ).ToList();

                _logger.LogInformation(
                    "Sending batch request to AI for {Count} chunks...", 
                    chunks.Count);

                var vectors = await embeddingService.GenerateEmbeddingsBatchAsync(
                    payloadForEmbedding,
                    EmbeddingType.Document,
                    cancellationToken);

                if (vectors.Count != chunks.Count)
                {
                    throw new InvalidOperationException(
                        $"Mismatch: Sent {chunks.Count} chunks, but AI returned {vectors.Count} vectors.");
                }

                var newChunks = new List<NoteChunk>(chunks.Count);
                for (int i = 0; i < chunks.Count; i++)
                {
                    newChunks.Add(new NoteChunk(note.Id, chunks[i], vectors[i]));
                }

                await _context.NoteChunks.AddRangeAsync(newChunks, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully saved {Count} embeddings for Note {NoteId}.", 
                    newChunks.Count, note.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for Note {NoteId}.", note.Id);
                throw;
            }
        }

        private List<string> SplitTextRecursively(string text, int chunkSize, int chunkOverlap)
        {
            var finalChunks = new List<string>();

            var splits = SplitInternal(text, _separators, chunkSize);

            return MergeSplits(splits, chunkSize, chunkOverlap);
        }

        private List<string> SplitInternal(string text, IList<string> separators, int chunkSize)
        {
            var finalSplits = new List<string>();
            var separator = separators.FirstOrDefault() ?? "";
            var nextSeparators = separators.Skip(1).ToList();

            if (text.Length <= chunkSize)
            {
                finalSplits.Add(text);
                return finalSplits;
            }

            if (string.IsNullOrEmpty(separator))
            {
                for (int i = 0; i < text.Length; i += chunkSize)
                {
                    int len = Math.Min(chunkSize, text.Length - i);
                    finalSplits.Add(text.Substring(i, len));
                }
                return finalSplits;
            }

            var parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                if (part.Length <= chunkSize)
                {
                    finalSplits.Add(part);
                }
                else
                {
                    finalSplits.AddRange(SplitInternal(part, nextSeparators, chunkSize));
                }
            }

            return finalSplits;
        }

        private List<string> MergeSplits(List<string> splits, int chunkSize, int chunkOverlap)
        {
            var docs = new List<string>();
            var currentDoc = new StringBuilder();
            var currentSplits = new List<string>(); 

            const string separator = " ";

            foreach (var split in splits)
            {
                if (currentDoc.Length + split.Length + separator.Length > chunkSize && currentDoc.Length > 0)
                {
                    docs.Add(currentDoc.ToString());

                    int overlapLength = 0;
                    var overlapBuffer = new StringBuilder();

                    for (int i = currentSplits.Count - 1; i >= 0; i--)
                    {
                        var s = currentSplits[i];
                        if (overlapLength + s.Length + separator.Length > chunkOverlap) break;

                        if (overlapBuffer.Length > 0) overlapBuffer.Insert(0, separator);
                        overlapBuffer.Insert(0, s);
                        overlapLength += s.Length + separator.Length;
                    }

                    currentDoc.Clear();
                    currentDoc.Append(overlapBuffer);

                    currentSplits.RemoveAll(x => !overlapBuffer.ToString().Contains(x));
                }

                if (currentDoc.Length > 0) currentDoc.Append(separator);
                currentDoc.Append(split);
                currentSplits.Add(split);
            }

            if (currentDoc.Length > 0)
            {
                docs.Add(currentDoc.ToString());
            }

            return docs;
        }
    }
}
