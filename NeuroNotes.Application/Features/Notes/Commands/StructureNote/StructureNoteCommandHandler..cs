using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;
using System.Globalization;

namespace NeuroNotes.Application.Features.Notes.Commands.StructureNote
{
    public class StructureNoteCommandHandler : IRequestHandler<StructureNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IPromptService _promptService;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly AIOptions _aiOptions; 
        private readonly ILogger<StructureNoteCommandHandler> _logger; 

        public StructureNoteCommandHandler(
            IApplicationDbContext context,
            IAIProviderFactory aiFactory,
            IPromptService promptService,
            INoteEmbeddingGenerator embeddingGenerator,
            IOptions<AIOptions> aiOptions,
            ILogger<StructureNoteCommandHandler> logger)
        {
            _context = context;
            _aiFactory = aiFactory;
            _promptService = promptService;
            _embeddingGenerator = embeddingGenerator;
            _aiOptions = aiOptions.Value;
            _logger = logger;
        }

        public async Task Handle(StructureNoteCommand request, CancellationToken cancellationToken)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for structuring.", 
                    request.NoteId);
                throw new NotFoundException(nameof(Note), request.NoteId);
            }

            _logger.LogInformation(
                "Starts structuring process for Note {NoteId}. User: {UserId}", 
                note.Id, note.UserId);

            if (string.IsNullOrWhiteSpace(note.RawText))
            {
                _logger.LogWarning(
                    "Structuring failed for Note {NoteId}. Reason: RawText is empty.", 
                    note.Id);
                throw new InvalidOperationException("Raw text is required for structure generation.");
            }

            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == note.UserId, cancellationToken);

            var providerType = (userAIProfile?.StructureProvider != 0)
                ? userAIProfile!.StructureProvider
                : _aiOptions.DefaultStructureProvider;

            var providerSettings = userAIProfile?.GetProviderSettings(providerType.ToString())
                                   ?? new Dictionary<string, string>();

            providerSettings["Temperature"] = _aiOptions.DefaultStructureTemperature.ToString(CultureInfo.InvariantCulture);

            var targetLanguage = !string.IsNullOrEmpty(userAIProfile?.AIOperationLanguage)
                ? userAIProfile!.AIOperationLanguage
                : _aiOptions.DefaultAIOperationLanguage;
            providerSettings["Language"] = targetLanguage;

            var systemPrompt = await _promptService.GetStructureSystemPromptAsync(note.UserId);
            var structureService = _aiFactory.GetStructureService(providerType);

            _logger.LogInformation(
                "Sending structure request to provider {Provider} for Note {NoteId}.", 
                providerType, note.Id);

            var structuredText = await structureService.StructureAsync(
                note.RawText,
                systemPrompt,
                providerSettings,
                cancellationToken);

            note.SetStructuredText(structuredText);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updating embeddings for StructuredText of Note {NoteId}.", note.Id);
            await _embeddingGenerator.UpdateEmbeddingsForSourceAsync(
                note, NoteChunkSourceType.StructuredText, cancellationToken);

            _logger.LogInformation(
                "Note {NoteId} structured and embedded successfully.", 
                note.Id);
        }
    }
}
