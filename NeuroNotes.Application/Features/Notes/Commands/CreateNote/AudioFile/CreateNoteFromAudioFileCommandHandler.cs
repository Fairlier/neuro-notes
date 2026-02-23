
using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Constants;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.BackgroundJobs;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.AudioFile
{
    public class CreateNoteFromAudioFileCommandHandler : IRequestHandler<CreateNoteFromAudioFileCommand, CreateNoteResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IFileSignatureValidator _fileValidator;
        private readonly ILogger<CreateNoteFromAudioFileCommandHandler> _logger; 

        public CreateNoteFromAudioFileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorage,
            IBackgroundJobService backgroundJobService,
            IFileSignatureValidator fileValidator,
            ILogger<CreateNoteFromAudioFileCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileStorage = fileStorage;
            _backgroundJobService = backgroundJobService;
            _fileValidator = fileValidator;
            _logger = logger;
        }

        public async Task<CreateNoteResponse> Handle(CreateNoteFromAudioFileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in CreateNoteFromAudioFile.");
                throw new UnauthorizedAccessException("User is not authorized to upload files.");
            }

            if (!request.FileStream.CanSeek)
            {
                throw new InvalidOperationException("File stream must be seekable.");
            }

            _logger.LogInformation(
                "Starts processing audio upload for User {UserId}. File: {FileName}, Size: {Size} bytes",
                userId, request.FileName, request.FileStream.Length);

            request.FileStream.Position = 0;

            if (!await _fileValidator.ValidateAudioFileAsync(request.FileStream, cancellationToken))
            {
                _logger.LogWarning(
                    "File signature validation failed for {FileName}. User: {UserId}", 
                    request.FileName, userId);

                throw new ArgumentException(
                    $"Invalid audio file signature. Actual content does not match supported formats: " +
                    $"{FileConstants.GetSupportedExtensionsString()}.");
            }

            request.FileStream.Position = 0;

            var fileKey = await _fileStorage.UploadFileAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                cancellationToken);

            _logger.LogInformation(
                "Audio file uploaded successfully. Key: {FileKey}", 
                fileKey);

            var note = new Note(request.Title, userId, NoteSourceType.AudioFile, fileKey);

            await _context.Notes.AddAsync(note, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note {NoteId} saved to database.",
                note.Id);

            _backgroundJobService.EnqueueTranscription(note.Id);

            _logger.LogInformation(
                "Transcription job enqueued for Note {NoteId}.",
                note.Id);

            return new CreateNoteResponse { Id = note.Id };
        }
    }
}
