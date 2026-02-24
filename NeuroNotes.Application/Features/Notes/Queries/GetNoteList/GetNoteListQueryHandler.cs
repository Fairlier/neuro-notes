
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class GetNoteListQueryHandler : IRequestHandler<GetNoteListQuery, NoteListResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly INoteSearchService _searchService;
        private readonly ILogger<GetNoteListQueryHandler> _logger;

        public GetNoteListQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ICurrentUserService currentUserService,
            INoteSearchService searchService,
            ILogger<GetNoteListQueryHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _searchService = searchService;
            _logger = logger;
        }

        public async Task<NoteListResponse> Handle(GetNoteListQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt. User is not authenticated.");
                throw new UnauthorizedAccessException("User is not authorized");
            }

            _logger.LogInformation(
                "Retrieving note list for User {UserId}. Search: '{SearchTerm}', Mode: {SearchMode}",
                userId, request.SearchTerm, request.SearchMode);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return await HandleSearchQueryAsync(request, userId, cancellationToken);
            }

            return await HandleStandardQueryAsync(request, userId, cancellationToken);
        }

        private async Task<NoteListResponse> HandleSearchQueryAsync(
            GetNoteListQuery request,
            string userId,
            CancellationToken cancellationToken)
        {
            var maxSearchResults = request.Page * request.PageSize * 2; 

            var relevantNoteIds = request.SearchMode == SearchMode.Semantic
                ? await _searchService.SemanticSearchAsync(userId, request.SearchTerm!, maxSearchResults, cancellationToken)
                : await _searchService.TextSearchAsync(userId, request.SearchTerm!, maxSearchResults, cancellationToken);

            if (relevantNoteIds.Count == 0)
            {
                return new NoteListResponse
                {
                    Notes = new List<NoteListItemDto>(),
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            var query = _context.Notes
                .AsNoTracking()
                .Where(n => relevantNoteIds.Contains(n.Id));

            query = ApplyFilters(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            var orderedIds = relevantNoteIds;

            var notesDict = await query
                .ProjectTo<NoteListItemDto>(_mapper.ConfigurationProvider)
                .ToDictionaryAsync(n => n.Id, cancellationToken);

            var orderedNotes = orderedIds
                .Where(id => notesDict.ContainsKey(id))
                .Select(id => notesDict[id])
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var totalPages = totalCount > 0
                ? (int)Math.Ceiling(totalCount / (double)request.PageSize)
                : 0;

            _logger.LogInformation(
                "Search completed. Found {Count} notes (page {Page}/{TotalPages}) for User {UserId}.",
                orderedNotes.Count, request.Page, totalPages, userId);

            return new NoteListResponse
            {
                Notes = orderedNotes,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        private async Task<NoteListResponse> HandleStandardQueryAsync(
            GetNoteListQuery request,
            string userId,
            CancellationToken cancellationToken)
        {
            var query = _context.Notes
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            query = ApplyFilters(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, request.SortBy, request.SortDirection);

            var skip = (request.Page - 1) * request.PageSize;
            query = query.Skip(skip).Take(request.PageSize);

            var notes = await query
                .ProjectTo<NoteListItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var totalPages = totalCount > 0
                ? (int)Math.Ceiling(totalCount / (double)request.PageSize)
                : 0;

            _logger.LogInformation(
                "Retrieved {Count} notes (page {Page}/{TotalPages}, total: {TotalCount}) for User {UserId}.",
                notes.Count, request.Page, totalPages, totalCount, userId);

            return new NoteListResponse
            {
                Notes = notes,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        private static IQueryable<Note> ApplyFilters(IQueryable<Note> query, GetNoteListQuery request)
        {
            if (request.Status.HasValue)
            {
                query = query.Where(n => n.Status == request.Status.Value);
            }

            if (request.Category.HasValue)
            {
                query = query.Where(n => n.Category == request.Category.Value);
            }

            if (request.CreatedFrom.HasValue)
            {
                var fromDate = request.CreatedFrom.Value.Date;
                query = query.Where(n => n.CreatedAt >= fromDate);
            }

            if (request.CreatedTo.HasValue)
            {
                var toDate = request.CreatedTo.Value.Date.AddDays(1);
                query = query.Where(n => n.CreatedAt < toDate);
            }

            if (request.UpdatedFrom.HasValue)
            {
                var fromDate = request.UpdatedFrom.Value.Date;
                query = query.Where(n => n.UpdatedAt >= fromDate);
            }

            if (request.UpdatedTo.HasValue)
            {
                var toDate = request.UpdatedTo.Value.Date.AddDays(1);
                query = query.Where(n => n.UpdatedAt < toDate);
            }

            return query;
        }

        private static IQueryable<Note> ApplySorting(
            IQueryable<Note> query,
            NoteSortBy sortBy,
            SortDirection direction)
        {
            return sortBy switch
            {
                NoteSortBy.Title => direction == SortDirection.Ascending
                    ? query.OrderBy(n => n.Title)
                    : query.OrderByDescending(n => n.Title),

                NoteSortBy.Status => direction == SortDirection.Ascending
                    ? query.OrderBy(n => n.Status)
                    : query.OrderByDescending(n => n.Status),

                NoteSortBy.Category => direction == SortDirection.Ascending
                    ? query.OrderBy(n => n.Category)
                    : query.OrderByDescending(n => n.Category),

                NoteSortBy.UpdatedAt => direction == SortDirection.Ascending
                    ? query.OrderBy(n => n.UpdatedAt ?? n.CreatedAt)
                    : query.OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt),

                NoteSortBy.CreatedAt or _ => direction == SortDirection.Ascending
                    ? query.OrderBy(n => n.CreatedAt)
                    : query.OrderByDescending(n => n.CreatedAt)
            };
        }
    }
}
