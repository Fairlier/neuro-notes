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

        private const int ChunkSize = 1000;
        private const int ChunkOverlap = 200;

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
            _logger.LogInformation("Starting full embedding generation for Note {NoteId}.", note.Id);

            try
            {
                await _context.NoteChunks
                    .Where(c => c.NoteId == note.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                var allChunks = new List<NoteChunk>();

                if (!string.IsNullOrWhiteSpace(note.Title))
                {
                    var titleChunks = await GenerateChunksForSourceAsync(
                        note, NoteChunkSourceType.Title, note.Title, cancellationToken);
                    allChunks.AddRange(titleChunks);
                }

                if (!string.IsNullOrWhiteSpace(note.RawText))
                {
                    var rawChunks = await GenerateChunksForSourceAsync(
                        note, NoteChunkSourceType.RawText, note.RawText, cancellationToken);
                    allChunks.AddRange(rawChunks);
                }

                if (!string.IsNullOrWhiteSpace(note.StructuredText))
                {
                    var structuredChunks = await GenerateChunksForSourceAsync(
                        note, NoteChunkSourceType.StructuredText, note.StructuredText, cancellationToken);
                    allChunks.AddRange(structuredChunks);
                }

                if (!string.IsNullOrWhiteSpace(note.SummaryText))
                {
                    var summaryChunks = await GenerateChunksForSourceAsync(
                        note, NoteChunkSourceType.SummaryText, note.SummaryText, cancellationToken);
                    allChunks.AddRange(summaryChunks);
                }

                if (allChunks.Count > 0)
                {
                    await _context.NoteChunks.AddRangeAsync(allChunks, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Successfully saved {Count} total embeddings for Note {NoteId} (Title: {TitleCount}, Raw: {RawCount}, Structured: {StructuredCount}, Summary: {SummaryCount}).",
                    allChunks.Count,
                    note.Id,
                    allChunks.Count(c => c.SourceType == NoteChunkSourceType.Title),
                    allChunks.Count(c => c.SourceType == NoteChunkSourceType.RawText),
                    allChunks.Count(c => c.SourceType == NoteChunkSourceType.StructuredText),
                    allChunks.Count(c => c.SourceType == NoteChunkSourceType.SummaryText));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for Note {NoteId}.", note.Id);
                throw;
            }
        }

        public async Task UpdateEmbeddingsForSourceAsync(
            Note note,
            NoteChunkSourceType sourceType,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Updating embeddings for Note {NoteId}, Source: {SourceType}.",
                note.Id, sourceType);

            try
            {
                var deletedCount = await _context.NoteChunks
                    .Where(c => c.NoteId == note.Id && c.SourceType == sourceType)
                    .ExecuteDeleteAsync(cancellationToken);

                _logger.LogDebug("Deleted {Count} old chunks for Source {SourceType}.", deletedCount, sourceType);

                var text = GetTextBySourceType(note, sourceType);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation(
                        "No text for source {SourceType} in Note {NoteId}. Chunks cleared.",
                        sourceType, note.Id);
                    return;
                }

                var chunks = await GenerateChunksForSourceAsync(note, sourceType, text, cancellationToken);

                if (chunks.Count > 0)
                {
                    await _context.NoteChunks.AddRangeAsync(chunks, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Updated {Count} embeddings for Note {NoteId}, Source: {SourceType}.",
                    chunks.Count, note.Id, sourceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update embeddings for Note {NoteId}, Source: {SourceType}.",
                    note.Id, sourceType);
                throw;
            }
        }

        private static string? GetTextBySourceType(Note note, NoteChunkSourceType sourceType)
        {
            return sourceType switch
            {
                NoteChunkSourceType.Title => note.Title,
                NoteChunkSourceType.RawText => note.RawText,
                NoteChunkSourceType.StructuredText => note.StructuredText,
                NoteChunkSourceType.SummaryText => note.SummaryText,
                _ => null
            };
        }

        private async Task<List<NoteChunk>> GenerateChunksForSourceAsync(
            Note note,
            NoteChunkSourceType sourceType,
            string text,
            CancellationToken cancellationToken)
        {
            var embeddingService = _embeddingFactory.Create();

            List<string> textChunks;
            if (sourceType == NoteChunkSourceType.Title)
            {
                textChunks = new List<string> { text };
            }
            else
            {
                textChunks = SplitTextRecursively(text, ChunkSize, ChunkOverlap);
            }

            if (textChunks.Count == 0)
                return new List<NoteChunk>();

            _logger.LogDebug(
                "Source {SourceType}: Split into {Count} chunks for Note {NoteId}.",
                sourceType, textChunks.Count, note.Id);

            var safeTitle = (note.Title ?? "Untitled").Replace("\n", " ").Trim();

            var payloadForEmbedding = textChunks.Select(chunkText =>
                $"[{sourceType}] Note: {safeTitle}\n{chunkText}"
            ).ToList();

            var vectors = await embeddingService.GenerateEmbeddingsBatchAsync(
                payloadForEmbedding,
                EmbeddingType.Document,
                cancellationToken);

            if (vectors.Count != textChunks.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding mismatch: Sent {textChunks.Count} chunks, received {vectors.Count} vectors.");
            }

            var noteChunks = new List<NoteChunk>(textChunks.Count);
            for (int i = 0; i < textChunks.Count; i++)
            {
                noteChunks.Add(new NoteChunk(note.Id, sourceType, textChunks[i], vectors[i]));
            }

            return noteChunks;
        }

        private List<string> SplitTextRecursively(string text, int chunkSize, int chunkOverlap)
        {
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
