using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Constants;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteSourceFile
{
    public class GetNoteSourceFileQueryHandler : IRequestHandler<GetNoteSourceFileQuery, NoteSourceFileResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<GetNoteSourceFileQueryHandler> _logger;

        public GetNoteSourceFileQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorage,
            ILogger<GetNoteSourceFileQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<NoteSourceFileResponse> Handle(
            GetNoteSourceFileQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Retrieving source file for Note {NoteId}, User {UserId}.",
                request.NoteId, userId);

            var note = await _context.Notes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    n => n.Id == request.NoteId && n.UserId == userId,
                    cancellationToken);

            if (note is null)
            {
                throw new NotFoundException(nameof(Note), request.NoteId);
            }

            if (string.IsNullOrEmpty(note.SourceFileUrl))
            {
                _logger.LogWarning("Note {NoteId} does not have a source file.", request.NoteId);
                throw new NotFoundException("Source file", request.NoteId);
            }

            var stream = new MemoryStream();
            await _fileStorage.DownloadToStreamAsync(
                note.SourceFileUrl,
                stream,
                cancellationToken);

            stream.Position = 0;

            _logger.LogInformation(
                "Source file retrieved for Note {NoteId}. Size: {Size} bytes.",
                request.NoteId, stream.Length);

            return new NoteSourceFileResponse
            {
                Stream = stream,
                ContentType = FileConstants.GetContentType(note.SourceFileUrl),
                FileName = Path.GetFileName(note.SourceFileUrl)
            };
        }
    }
}
