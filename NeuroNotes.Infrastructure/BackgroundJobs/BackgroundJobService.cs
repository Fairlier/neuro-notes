using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Features.Notes.Commands.StructureNote;
using NeuroNotes.Application.Features.Notes.Commands.SummarizeNote;
using NeuroNotes.Application.Features.Notes.Commands.TranscribeNote;
using NeuroNotes.Application.Interfaces.BackgroundJobs;

namespace NeuroNotes.Infrastructure.BackgroundJobs
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IBackgroundJobClient backgroundJobClient,
            ILogger<BackgroundJobService> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public void EnqueueTranscription(Guid noteId)
        {
            if (noteId == Guid.Empty)
            {
                _logger.LogError("Attempted to enqueue Transcription for empty NoteId.");
                return;
            }

            _logger.LogInformation("Enqueuing Transcription background job for Note {NoteId}...", noteId);

            var jobId = _backgroundJobClient.Enqueue<IMediator>(
                mediator => mediator.Send(new TranscribeNoteCommand(noteId), CancellationToken.None));

            _logger.LogInformation("Transcription job enqueued successfully. Hangfire JobId: {JobId}, NoteId: {NoteId}", jobId, noteId);
        }

        public void EnqueueStructureGeneration(Guid noteId)
        {
            if (noteId == Guid.Empty)
            {
                _logger.LogError("Attempted to enqueue Structure Generation for empty NoteId.");
                return;
            }

            _logger.LogInformation("Enqueuing Structure Generation background job for Note {NoteId}...", noteId);

            var jobId = _backgroundJobClient.Enqueue<IMediator>(
                mediator => mediator.Send(new StructureNoteCommand(noteId), CancellationToken.None));

            _logger.LogInformation("Structure Generation job enqueued successfully. Hangfire JobId: {JobId}, NoteId: {NoteId}", jobId, noteId);
        }

        public void EnqueueSummaryGeneration(Guid noteId)
        {
            if (noteId == Guid.Empty)
            {
                _logger.LogError("Attempted to enqueue Summary Generation for empty NoteId.");
                return;
            }

            _logger.LogInformation("Enqueuing Summary Generation background job for Note {NoteId}...", noteId);

            var jobId = _backgroundJobClient.Enqueue<IMediator>(
                mediator => mediator.Send(new SummarizeNoteCommand(noteId), CancellationToken.None));

            _logger.LogInformation("Summary Generation job enqueued successfully. Hangfire JobId: {JobId}, NoteId: {NoteId}", jobId, noteId);
        }
    }
}
