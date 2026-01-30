namespace NeuroNotes.Application.Interfaces.BackgroundJobs
{
    public interface IBackgroundJobService
    {
        void EnqueueTranscription(Guid noteId);

        void EnqueueStructureGeneration(Guid noteId);

        void EnqueueSummaryGeneration(Guid noteId);
    }
}
