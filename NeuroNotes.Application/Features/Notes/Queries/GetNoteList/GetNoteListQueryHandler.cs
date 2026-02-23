
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class GetNoteListQueryHandler : IRequestHandler<GetNoteListQuery, NoteListResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetNoteListQueryHandler> _logger;

        public GetNoteListQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<GetNoteListQueryHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
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

            _logger.LogInformation("Retrieving note list for User {UserId}.", userId);

            var notes = await _context.Notes
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
                .ProjectTo<NoteListItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} notes for User {UserId}.", notes.Count, userId);

            return new NoteListResponse { Notes = notes };
        }
    }
}
