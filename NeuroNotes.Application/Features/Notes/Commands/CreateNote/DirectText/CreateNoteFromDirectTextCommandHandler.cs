using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.DirectText
{
    public class CreateNoteFromDirectTextCommandHandler : IRequestHandler<CreateNoteFromDirectTextCommand, CreateNoteResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly ILogger<CreateNoteFromDirectTextCommandHandler> _logger;

        public CreateNoteFromDirectTextCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            INoteEmbeddingGenerator embeddingGenerator,
            ILogger<CreateNoteFromDirectTextCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _embeddingGenerator = embeddingGenerator;
            _logger = logger;
        }

        public async Task<CreateNoteResponse> Handle(CreateNoteFromDirectTextCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in CreateNoteFromDirectText.");
                throw new UnauthorizedAccessException("User is not authorized to create notes.");
            }

            _logger.LogInformation(
                "Starts creating note from direct text for User {UserId}. Title: {Title}",
                userId, request.Title);

            var note = new Note(request.Title, userId, NoteSourceType.DirectText);

            note.SetRawText(request.Content);

            await _context.Notes.AddAsync(note, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generating embeddings for Note {NoteId}.", note.Id);
            await _embeddingGenerator.GenerateAndSaveEmbeddingsAsync(note, cancellationToken);

            _logger.LogInformation(
                "Note created successfully. Id: {NoteId}", 
                note.Id);

            return new CreateNoteResponse { Id = note.Id };
        }
    }
}
