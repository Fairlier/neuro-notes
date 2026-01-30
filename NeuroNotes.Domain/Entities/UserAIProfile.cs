using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;
using System.Text.Json;

namespace NeuroNotes.Domain.Entities
{
    public class UserAIProfile : BaseEntity
    {
        public string UserId { get; private set; } = string.Empty;
        public string AIOperationLanguage { get; private set; } = string.Empty;

        public TranscriptionProviderType TranscriptionProvider { get; private set; }
        public ChatProviderType ChatProvider { get; private set; } 
        public StructureProviderType StructureProvider { get; private set; } 
        public SummaryProviderType SummaryProvider { get; private set; }

        public string ProviderSettingsJson { get; private set; } = "{}";
        public string? CustomTranscriptionPrompt { get; private set; }
        public string? CustomStructurePrompt { get; private set; } 
        public string? CustomSummaryPrompt { get; private set; }
        public string? CustomChatPrompt { get; private set; }

        protected UserAIProfile() { }

        public UserAIProfile(string userId)
        {
            UserId = userId;
        }

        public void UpdatePreferences(
            string aiOperationLanguage,
            TranscriptionProviderType transProvider,
            ChatProviderType chatProvider,
            StructureProviderType structureProvider, 
            SummaryProviderType summaryProvider      
            )
        {
            AIOperationLanguage = aiOperationLanguage;
            TranscriptionProvider = transProvider;
            ChatProvider = chatProvider;
            StructureProvider = structureProvider;
            SummaryProvider = summaryProvider;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProviderSettings(string providerName, Dictionary<string, string> settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            Dictionary<string, Dictionary<string, string>> currentData;

            try
            {
                currentData = string.IsNullOrEmpty(ProviderSettingsJson)
                    ? new Dictionary<string, Dictionary<string, string>>()
                    : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(ProviderSettingsJson)
                      ?? new Dictionary<string, Dictionary<string, string>>();
            }
            catch
            {
                currentData = new Dictionary<string, Dictionary<string, string>>();
            }

            if (currentData.ContainsKey(providerName))
            {
                foreach (var kvp in settings)
                {
                    currentData[providerName][kvp.Key] = kvp.Value;
                }
            }
            else
            {
                currentData[providerName] = settings;
            }

            ProviderSettingsJson = JsonSerializer.Serialize(currentData, options);
            UpdatedAt = DateTime.UtcNow;
        }

        public Dictionary<string, string> GetProviderSettings(string providerName)
        {
            if (string.IsNullOrEmpty(ProviderSettingsJson)) return new Dictionary<string, string>();

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(ProviderSettingsJson);
                return data != null && data.TryGetValue(providerName, out var settings) ? settings : new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public void SetPrompts(
            string? transPrompt,
            string? analysisPrompt,
            string? chatPrompt,
            string? summaryPrompt)
        {
            CustomTranscriptionPrompt = transPrompt;
            CustomStructurePrompt = analysisPrompt;
            CustomChatPrompt = chatPrompt;
            CustomSummaryPrompt = summaryPrompt; 
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
