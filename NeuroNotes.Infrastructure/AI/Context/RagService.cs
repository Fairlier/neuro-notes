using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Context;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Enums;
using Pgvector.EntityFrameworkCore;
using System.Text;

namespace NeuroNotes.Infrastructure.AI.Context
{
    public class RagService : IRagService
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmbeddingModelFactory _embeddingFactory;
        private readonly EmbeddingOptions _options; 
        private readonly ILogger<RagService> _logger;

        public RagService(
            IApplicationDbContext context,
            IEmbeddingModelFactory embeddingFactory,
            IOptions<EmbeddingOptions> embeddingOptions,
            ILogger<RagService> logger)
        {
            _context = context;
            _embeddingFactory = embeddingFactory;
            _options = embeddingOptions.Value;
            _logger = logger;
        }

        public async Task<string> GetRelevantContextAsync(string userId, string query, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starts RAG retrieval for User {UserId}. Query: {Query}", 
                userId, query);

            var (limit, maxDistance) = GetSearchParameters();

            var embeddingService = _embeddingFactory.Create();
            var queryVector = await embeddingService.GenerateEmbeddingAsync(
                query,
                EmbeddingType.Query,
                cancellationToken);

            int databaseFetchLimit = limit * 3;

            var searchResults = await _context.NoteChunks
                .AsNoTracking()
                .Include(c => c.Note)
                .Where(c => c.Note!.UserId == userId)
                .Select(c => new
                {
                    Chunk = c,
                    Distance = c.Embedding!.CosineDistance(queryVector)
                })
                .Where(x => x.Distance <= maxDistance)
                .OrderBy(x => x.Distance)
                .Take(databaseFetchLimit)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "RAG search completed using {Provider}. Found {Count} candidates (Threshold: {Threshold}, Limit: {Limit}).",
                _options.DefaultEmbeddingProvider, searchResults.Count, maxDistance, limit);

            if (searchResults.Count == 0)
            {
                _logger.LogWarning(
                    "No relevant context found within distance {Threshold}.", 
                    maxDistance);
                return string.Empty;
            }

            var uniqueNoteResults = searchResults
                .GroupBy(r => r.Chunk.NoteId)
                .Select(g => g.OrderBy(x => x.Distance).First())
                .OrderBy(x => x.Distance)
                .Take(limit)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<documents>");
            sb.AppendLine("Use the following context snippets to answer the user question.");
            foreach (var item in uniqueNoteResults)
            {
                var note = item.Chunk.Note;
                var safeTitle = (note?.Title ?? "Untitled").Replace("\n", " ").Replace("\r", "");

                _logger.LogDebug(
                    "RAG Include: '{Title}' [{Source}] (Distance: {Dist:F4})",
                    safeTitle, item.Chunk.SourceType, item.Distance);

                sb.AppendLine($"""
                    <document source="{safeTitle}" type="{item.Chunk.SourceType}" date="{note?.CreatedAt:yyyy-MM-dd}">
                    {item.Chunk.Content}
                    </document>
                    """);
            }
            sb.AppendLine("</documents>");

            return sb.ToString();
        }

        private (int Limit, double MaxDistance) GetSearchParameters()
        {
            return _options.DefaultEmbeddingProvider switch
            {
                EmbeddingProviderType.GemmaLocal => (
                    _options.GemmaLocal.SearchResultLimit,
                    _options.GemmaLocal.MaxCosineDistance
                ),
                _ => (5, 0.7)
            };
        }
    }
}
