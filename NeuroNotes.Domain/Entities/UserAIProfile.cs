using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;
using NeuroNotes.Domain.ValueObjects;
using System.Text.Json;

namespace NeuroNotes.Domain.Entities
{
    public class UserAIProfile : BaseEntity
    {
        public string UserId { get; private set; } = string.Empty;
        public string AIOperationLanguage { get; private set; } = string.Empty;

        public TranscriptionProviderType TranscriptionProvider { get; private set; }
        public StructureProviderType StructureProvider { get; private set; } 
        public SummaryProviderType SummaryProvider { get; private set; }
        public ChatProviderType GlobalChatProvider { get; private set; }
        public ChatProviderType NoteChatProvider { get; private set; }

        public string ProviderSettingsJson { get; private set; } = "{}";

        public AIOperationSettings Transcription { get; private set; } = AIOperationSettings.Default(true);
        public AIOperationSettings Structuring { get; private set; } = AIOperationSettings.Default(true);
        public AIOperationSettings Summarization { get; private set; } = AIOperationSettings.Default(true);
        public AIOperationSettings GlobalChat { get; private set; } = AIOperationSettings.Default(null);
        public AIOperationSettings NoteChat { get; private set; } = AIOperationSettings.Default(null);

        public AIOperationSettings Classification { get; private set; } = AIOperationSettings.Default(true);

        protected UserAIProfile() { }

        public UserAIProfile(string userId)
        {
            UserId = userId;
        }

        public void UpdatePreferences(
            string aiOperationLanguage,
            TranscriptionProviderType transProvider,
            StructureProviderType structureProvider, 
            SummaryProviderType summaryProvider ,
            ChatProviderType globalChatProvider,
            ChatProviderType noteChatProvider
            )
        {
            AIOperationLanguage = aiOperationLanguage;
            TranscriptionProvider = transProvider;
            StructureProvider = structureProvider;
            SummaryProvider = summaryProvider;
            GlobalChatProvider = globalChatProvider;
            NoteChatProvider = noteChatProvider;
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
                return data is not null && data.TryGetValue(providerName, out var settings) ? settings : new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public void UpdateTranscription(string? targetLanguage, string? customPrompt, bool useCustomPrompt, bool? isAutomatic)
        {
            Transcription = Transcription with { TargetLanguage = targetLanguage, CustomPrompt = customPrompt, 
                UseCustomPrompt = useCustomPrompt, IsAutomatic = isAutomatic };
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStructuring(string? targetLanguage, string? customPrompt, bool useCustomPrompt, bool? isAutomatic)
        {
            Structuring = Structuring with { TargetLanguage = targetLanguage, CustomPrompt = customPrompt, 
                UseCustomPrompt = useCustomPrompt, IsAutomatic = isAutomatic };
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateSummarization(string? targetLanguage, string? customPrompt, bool useCustomPrompt, bool? isAutomatic)
        {
            Summarization = Summarization with { TargetLanguage = targetLanguage, CustomPrompt = customPrompt, 
                UseCustomPrompt = useCustomPrompt, IsAutomatic = isAutomatic };
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateGlobalChat(string? targetLanguage, string? customPrompt, bool useCustomPrompt)
        {
            GlobalChat = GlobalChat with { TargetLanguage = targetLanguage, CustomPrompt = customPrompt, 
                UseCustomPrompt = useCustomPrompt, IsAutomatic = null };
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateNoteChat(string? targetLanguage, string? customPrompt, bool useCustomPrompt)
        {
            NoteChat = NoteChat with { TargetLanguage = targetLanguage, CustomPrompt = customPrompt, 
                UseCustomPrompt = useCustomPrompt, IsAutomatic = null };
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateClassification(bool? isAutomatic)
        {
            Classification = Classification with { IsAutomatic = isAutomatic };
            UpdatedAt = DateTime.UtcNow;
        }

        public void ResetToDefaults(
            string aiOperationLanguage,
            TranscriptionProviderType transProvider,
            StructureProviderType structureProvider,
            SummaryProviderType summaryProvider,
            ChatProviderType globalChatProvider,
            ChatProviderType noteChatProvider)
        {
            AIOperationLanguage = aiOperationLanguage;
            TranscriptionProvider = transProvider;
            StructureProvider = structureProvider;
            SummaryProvider = summaryProvider;
            GlobalChatProvider = globalChatProvider;
            NoteChatProvider = noteChatProvider;

            ProviderSettingsJson = "{}";

            Transcription = AIOperationSettings.Default(true);
            Structuring = AIOperationSettings.Default(false);
            Summarization = AIOperationSettings.Default(false);
            GlobalChat = AIOperationSettings.Default(null);
            NoteChat = AIOperationSettings.Default(null);

            Classification = AIOperationSettings.Default(true);

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
