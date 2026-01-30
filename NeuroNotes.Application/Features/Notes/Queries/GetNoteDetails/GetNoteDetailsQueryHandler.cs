using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteDetails
{
    public class GetNoteDetailsQueryHandler : IRequestHandler<GetNoteDetailsQuery, NoteDetailsResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetNoteDetailsQueryHandler> _logger;

        public GetNoteDetailsQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<GetNoteDetailsQueryHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<NoteDetailsResponse> Handle(GetNoteDetailsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt. User is not authenticated.");
                throw new UnauthorizedAccessException("User is not authorized");
            }

            _logger.LogInformation("Retrieving note details for Note {NoteId}. User: {UserId}", request.Id, userId);

            var note = await _context.Notes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning("Failed to retrieve note details. Reason: Note {NoteId} not found for User {UserId}.", request.Id, userId);
                throw new NotFoundException(nameof(Note), request.Id);
            }

            _logger.LogInformation("Retrieved note details successfully for Note {NoteId}.", request.Id);

            return _mapper.Map<NoteDetailsResponse>(note);
        }
    }
}
