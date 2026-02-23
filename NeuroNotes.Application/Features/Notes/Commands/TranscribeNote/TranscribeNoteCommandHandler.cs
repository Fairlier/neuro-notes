using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Constants;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Commands.TranscribeNote
{
    public class TranscribeNoteCommandHandler : IRequestHandler<TranscribeNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IAIProviderFactory _aiFactory;
        private readonly IPromptService _promptService;
        private readonly IMimeTypeDetector _mimeTypeDetector;
        private readonly INoteEmbeddingGenerator _embeddingGenerator;
        private readonly INoteCategoryClassifier _categoryClassifier;
        private readonly AIOptions _aiOptions;
        private readonly ILogger<TranscribeNoteCommandHandler> _logger;

        public TranscribeNoteCommandHandler(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            IAIProviderFactory aiFactory,
            IPromptService promptService,
            IMimeTypeDetector mimeTypeDetector,
            INoteEmbeddingGenerator embeddingGenerator,
            INoteCategoryClassifier categoryClassifier,
            IOptions<AIOptions> aiOptions,
            ILogger<TranscribeNoteCommandHandler> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _aiFactory = aiFactory;
            _promptService = promptService;
            _mimeTypeDetector = mimeTypeDetector;
            _embeddingGenerator = embeddingGenerator;
            _categoryClassifier = categoryClassifier;
            _aiOptions = aiOptions.Value;
            _logger = logger;
        }

        public async Task Handle(TranscribeNoteCommand request, CancellationToken cancellationToken)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken);

            if (note is null)
            {
                _logger.LogWarning(
                    "Note {NoteId} not found during transcription request.", 
                    request.NoteId);
                throw new NotFoundException(nameof(Note), request.NoteId);
            }

            _logger.LogInformation(
                "Starts transcription process for Note {NoteId}. User: {UserId}", 
                note.Id, note.UserId);

            if (string.IsNullOrEmpty(note.SourceFileUrl))
            {
                var msg = "Source file URL is missing.";
                _logger.LogWarning(
                    "Transcription failed for Note {NoteId}: {Reason}", 
                    note.Id, msg);

                note.FailProcessing(msg);
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var userAIProfile = await _context.UserAIProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == note.UserId, cancellationToken);

            var providerType = (userAIProfile?.TranscriptionProvider != 0)
                ? userAIProfile!.TranscriptionProvider
                : _aiOptions.DefaultTranscriptionProvider;

            var providerSettings = userAIProfile?.GetProviderSettings(providerType.ToString())
                                   ?? new Dictionary<string, string>();

            var targetLanguage = !string.IsNullOrEmpty(userAIProfile?.AIOperationLanguage)
                ? userAIProfile!.AIOperationLanguage
                : _aiOptions.DefaultAIOperationLanguage;
            providerSettings["Language"] = targetLanguage;

            bool isLocalProvider = providerType.ToString().Contains("Local", StringComparison.OrdinalIgnoreCase);

            if (!isLocalProvider)
            {
                long remoteFileSize = await _fileStorage.GetFileSizeAsync(note.SourceFileUrl, cancellationToken);

                if (remoteFileSize > FileConstants.MaxAudioApiProcessingSizeBytes)
                {
                    var errorMsg = 
                        $"Audio file size ({remoteFileSize / 1024 / 1024} MB) " +
                        $"exceeds the limit ({FileConstants.MaxAudioApiProcessingSizeMbString}) " +
                        $"for external provider {providerType}. Please use a Local provider or compress the file.";

                    _logger.LogWarning(
                        "Transcription rejected for Note {NoteId}. Reason: File size limit for remote provider.", 
                        note.Id);

                    note.FailProcessing(errorMsg);
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            var systemPrompt = await _promptService.GetTranscriptionSystemPromptAsync(note.UserId);
            var transcriptionService = _aiFactory.GetTranscriptionService(providerType);

            var tempFile = Path.GetTempFileName();
            _logger.LogInformation(
                "Downloading file for Note {NoteId} to temporary path.", 
                note.Id);

            try
            {
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    await _fileStorage.DownloadToStreamAsync(note.SourceFileUrl, fs, cancellationToken);
                }

                using (var readStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                {
                    var fileName = Path.GetFileName(note.SourceFileUrl);
                    var contentType = _mimeTypeDetector.GetMimeType(readStream, fileName);

                    if (readStream.CanSeek) readStream.Position = 0;

                    _logger.LogInformation(
                        "Sending audio to provider {Provider} for Note {NoteId}. Size: {Size} bytes, Local: {IsLocal}",
                        providerType, note.Id, readStream.Length, isLocalProvider);

                    var rawText = await transcriptionService.TranscribeAsync(
                        readStream,
                        fileName,
                        contentType,
                        systemPrompt,
                        providerSettings,
                        cancellationToken);

                    note.SetRawText(rawText);

                    _logger.LogInformation("Classifying category for Note {NoteId}.", note.Id);
                    var (category, confidence) = await _categoryClassifier.ClassifyWithConfidenceAsync(
                        rawText, cancellationToken);

                    note.SetCategory(category);
                    _logger.LogInformation(
                        "Note {NoteId} classified as {Category} with confidence {Confidence:F4}.",
                        note.Id, category, confidence);

                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Updating embeddings for RawText of Note {NoteId}.", note.Id);
                    await _embeddingGenerator.UpdateEmbeddingsForSourceAsync(
                        note, NoteChunkSourceType.RawText, cancellationToken);

                    _logger.LogInformation(
                        "Transcription completed successfully for Note {NoteId}.", 
                        note.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcription failed for Note {NoteId} with unexpected error.", note.Id);
                note.FailProcessing($"Transcription failed: {ex.Message}");
                await _context.SaveChangesAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }
    }
}
