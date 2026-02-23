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

namespace NeuroNotes.Application.Features.Notes.Commands.SummarizeNote
{
    public class SummarizeNoteCommandHandler : IRequestHandler<SummarizeNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IPromptService _promptService;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly AIOptions _aiOptions;
        private readonly ILogger<SummarizeNoteCommandHandler> _logger;

        public SummarizeNoteCommandHandler(
           IApplicationDbContext context,
           IAIProviderFactory aiFactory,
           IPromptService promptService,
           INoteEmbeddingGenerator embeddingGenerator,
           IOptions<AIOptions> aiOptions,
           ILogger<SummarizeNoteCommandHandler> logger)
        {
            _context = context;
            _aiFactory = aiFactory;
            _promptService = promptService;
            _embeddingGenerator = embeddingGenerator;
            _aiOptions = aiOptions.Value;
            _logger = logger;
        }

        public async Task Handle(SummarizeNoteCommand request, CancellationToken cancellationToken)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found for summarization.",
                    request.NoteId);
                throw new NotFoundException(nameof(Note), request.NoteId);
            }

            _logger.LogInformation(
                "Starts summarizing note {NoteId} for User {UserId}.",
                note.Id, note.UserId);

            if (string.IsNullOrWhiteSpace(note.StructuredText))
            {
                _logger.LogWarning(
                    "Summarization failed for Note {NoteId}. Reason: StructuredText is empty.",
                    note.Id);
                throw new InvalidOperationException("Structured text is required for summary generation.");
            }

            note.StartProcessing();
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == note.UserId, cancellationToken);

                var providerType = (userAIProfile?.SummaryProvider != 0)
                    ? userAIProfile!.SummaryProvider
                    : _aiOptions.DefaultSummaryProvider;

                var providerSettings = userAIProfile?.GetProviderSettings(providerType.ToString())
                                       ?? new Dictionary<string, string>();

                var targetLanguage = !string.IsNullOrEmpty(userAIProfile?.AIOperationLanguage)
                    ? userAIProfile!.AIOperationLanguage
                    : _aiOptions.DefaultAIOperationLanguage;
                providerSettings["Language"] = targetLanguage;

                var systemPrompt = await _promptService.GetSummarySystemPromptAsync(note.UserId);
                var summaryService = _aiFactory.GetSummaryService(providerType);

                _logger.LogInformation(
                    "Sending summarization request to provider {Provider} for Note {NoteId}.",
                    providerType, note.Id);

                var summaryText = await summaryService.SummarizeAsync(
                    structureText: note.StructuredText,
                    systemPrompt: systemPrompt,
                    providerSettings: providerSettings,
                    cancellationToken: cancellationToken);

                note.SetSummaryText(summaryText);

                _logger.LogInformation(
                    "Updating embeddings for SummaryText of Note {NoteId}.",
                    note.Id);

                await _embeddingGenerator.UpdateEmbeddingsForSourceAsync(
                    note, NoteChunkSourceType.SummaryText, cancellationToken);

                note.FinishProcessing();
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Note {NoteId} summarized successfully. " +
                    "Status: {Status}, IsProcessing: {IsProcessing}",
                    note.Id, note.Status, note.IsProcessing);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Summarization failed for Note {NoteId}.",
                    note.Id);

                note.FailProcessing($"Summarization failed: {ex.Message}");
                await _context.SaveChangesAsync(CancellationToken.None);
                throw;
            }
        }
    }
}
