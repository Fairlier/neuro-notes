namespace NeuroNotes.Application.Interfaces.AI.Prompting
{
    public interface IPromptService
    {
        Task<string> GetTranscriptionSystemPromptAsync(string userId);
        Task<string> GetStructureSystemPromptAsync(string userId);
        Task<string> GetSummarySystemPromptAsync(string userId);
        Task<string> GetChatSystemPromptAsync(string userId);
    }
}
