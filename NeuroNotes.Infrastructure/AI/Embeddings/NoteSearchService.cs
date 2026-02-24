
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Enums;
using Pgvector.EntityFrameworkCore;

namespace NeuroNotes.Infrastructure.AI.Embeddings
{
    public class NoteSearchService : INoteSearchService
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmbeddingModelFactory _embeddingFactory;
        private readonly ILogger<NoteSearchService> _logger;

        private const double RelevanceThreshold = 0.8;

        public NoteSearchService(
            IApplicationDbContext context,
            IEmbeddingModelFactory embeddingFactory,
            ILogger<NoteSearchService> logger)
        {
            _context = context;
            _embeddingFactory = embeddingFactory;
            _logger = logger;
        }

        public async Task<List<Guid>> SemanticSearchAsync(
            string userId,
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Guid>();
            }

            _logger.LogInformation(
                "Performing semantic search for User {UserId}. Query: '{Query}', Limit: {Limit}, Threshold: {Threshold}",
                userId, query, limit, RelevanceThreshold);

            try
            {
                var embeddingService = _embeddingFactory.Create();
                var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(
                    query,
                    EmbeddingType.Query,
                    cancellationToken);

                var userNoteIds = await _context.Notes
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .Select(n => n.Id)
                    .ToListAsync(cancellationToken);

                if (userNoteIds.Count == 0)
                {
                    return new List<Guid>();
                }

                var searchResults = await _context.NoteChunks
                    .AsNoTracking()
                    .Where(c => userNoteIds.Contains(c.NoteId) && c.Embedding != null)
                    .Select(c => new
                    {
                        c.NoteId,
                        Distance = c.Embedding!.CosineDistance(queryEmbedding)
                    })
                    .Where(x => x.Distance < RelevanceThreshold) 
                    .OrderBy(x => x.Distance)
                    .Take(limit * 3)
                    .ToListAsync(cancellationToken);

                var noteIds = searchResults
                    .GroupBy(x => x.NoteId)
                    .Select(g => new
                    {
                        NoteId = g.Key,
                        BestDistance = g.Min(x => x.Distance)
                    })
                    .OrderBy(x => x.BestDistance)
                    .Take(limit)
                    .Select(x => x.NoteId)
                    .ToList();

                _logger.LogInformation(
                    "Semantic search completed. Found {Count} relevant notes (threshold: {Threshold}) for User {UserId}.",
                    noteIds.Count, RelevanceThreshold, userId);

                if (searchResults.Count > 0)
                {
                    var bestDistance = searchResults.Min(x => x.Distance);
                    var worstDistance = searchResults.Max(x => x.Distance);
                    _logger.LogDebug(
                        "Search distances - Best: {Best:F4}, Worst: {Worst:F4}",
                        bestDistance, worstDistance);
                }

                return noteIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for User {UserId}.", userId);
                throw;
            }
        }

        public async Task<List<Guid>> TextSearchAsync(
            string userId,
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Guid>();
            }

            _logger.LogInformation(
                "Performing text search for User {UserId}. Query: '{Query}', Limit: {Limit}",
                userId, query, limit);

            var term = query.Trim().ToLower();

            var userNoteIds = await _context.Notes
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .Select(n => n.Id)
                .ToListAsync(cancellationToken);

            if (userNoteIds.Count == 0)
            {
                return new List<Guid>();
            }

            var noteIds = await _context.NoteChunks
                .AsNoTracking()
                .Where(c => userNoteIds.Contains(c.NoteId) &&
                            c.Content.ToLower().Contains(term))
                .Select(c => c.NoteId)
                .Distinct()
                .Take(limit)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Text search completed. Found {Count} notes for User {UserId}.",
                noteIds.Count, userId);

            return noteIds;
        }
    }
}
