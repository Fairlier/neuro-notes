using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.ValueObjects;
using System.Text.Json;

namespace NeuroNotes.Infrastructure.AI.Prompting
{
    public class PromptService : IPromptService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<PromptService> _logger;
        private readonly IMemoryCache _cache;
        private readonly AIOptions _aiOptions;
        private readonly string _basePath;

        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.High
        };

        public PromptService(
            IApplicationDbContext context,
            ILogger<PromptService> logger,
            IWebHostEnvironment env,
            IMemoryCache cache,
            IOptions<AIOptions> aiOptions)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _aiOptions = aiOptions.Value;
            _basePath = Path.Combine(env.ContentRootPath, "Resources", "Prompts");
        }

        public Task<string> GetTranscriptionSystemPromptAsync(string userId)
            => GetPromptInternalAsync(
                userId,
                folderName: "Transcription",
                fileNamePrefix: "transcription",
                operationSettingsSelector: p => p.Transcription);

        public Task<string> GetStructureSystemPromptAsync(string userId)
            => GetPromptInternalAsync(
                userId,
                folderName: "Structure",
                fileNamePrefix: "structure",
                operationSettingsSelector: p => p.Structuring);

        public Task<string> GetSummarySystemPromptAsync(string userId)
            => GetPromptInternalAsync(
                userId,
                folderName: "Summary",
                fileNamePrefix: "summary",
                operationSettingsSelector: p => p.Summarization);

        public Task<string> GetGlobalChatSystemPromptAsync(string userId)
            => GetPromptInternalAsync(
                userId,
                folderName: "GlobalChat",
                fileNamePrefix: "global_chat",
                operationSettingsSelector: p => p.GlobalChat);

        public Task<string> GetNoteChatSystemPromptAsync(string userId)
            => GetPromptInternalAsync(
                userId,
                folderName: "NoteChat",
                fileNamePrefix: "note_chat",
                operationSettingsSelector: p => p.NoteChat);

        private async Task<string> GetPromptInternalAsync(
           string userId,
           string folderName,
           string fileNamePrefix,
           Func<UserAIProfile, AIOperationSettings?> operationSettingsSelector)
        {
            _logger.LogDebug("Resolving prompt for '{Category}'. User: {UserId}", folderName, userId);

            var profile = await GetUserAIProfileAsync(userId);
            var operationSettings = profile != null ? operationSettingsSelector(profile) : null;

            if (operationSettings is { UseCustomPrompt: true } &&
                !string.IsNullOrWhiteSpace(operationSettings.CustomPrompt))
            {
                _logger.LogInformation(
                    "Using custom prompt for '{Category}' (User: {UserId}).",
                    folderName, userId);
                return operationSettings.CustomPrompt;
            }

            var targetLanguage = ResolveTargetLanguage(profile, operationSettings);
            _logger.LogDebug("Resolved target language '{Language}' for '{Category}'.", targetLanguage, folderName);

            return await GetCachedSystemPromptAsync(folderName, fileNamePrefix, targetLanguage);
        }

        private string ResolveTargetLanguage(
            UserAIProfile? profile,
            AIOperationSettings? operationSettings)
        {
            if (!string.IsNullOrWhiteSpace(operationSettings?.TargetLanguage))
            {
                _logger.LogDebug("Using operation-specific language: {Language}", operationSettings.TargetLanguage);
                return operationSettings.TargetLanguage.ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(profile?.AIOperationLanguage))
            {
                _logger.LogDebug("Using profile AI operation language: {Language}", profile.AIOperationLanguage);
                return profile.AIOperationLanguage.ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(_aiOptions.DefaultAIOperationLanguage))
            {
                _logger.LogDebug("Using application default language: {Language}", _aiOptions.DefaultAIOperationLanguage);
                return _aiOptions.DefaultAIOperationLanguage.ToLowerInvariant();
            }

            var errorMsg = "No AI operation language configured. " +
                "Please set: UserAIProfile.AIOperationLanguage, " +
                "Operation.TargetLanguage, or AIOptions.DefaultAIOperationLanguage";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        private async Task<string> GetCachedSystemPromptAsync(
            string folder,
            string prefix,
            string language)
        {
            var cacheKey = $"Prompt_{folder}_{prefix}_{language}";

            try
            {
                var prompt = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    _logger.LogInformation(
                        "Cache MISS for prompt: {Key}. Loading from disk.", cacheKey);
                    entry.SetOptions(_cacheEntryOptions);
                    return await LoadSystemPromptFromDiskAsync(folder, prefix, language);
                });

                return prompt ?? throw new InvalidOperationException(
                    $"Prompt service returned null for cache key {cacheKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load prompt from cache. Key: {Key}, Folder: {Folder}, Language: {Language}",
                    cacheKey, folder, language);
                throw;
            }
        }

        private async Task<string> LoadSystemPromptFromDiskAsync(
            string folder,
            string prefix,
            string language)
        {
            var filePath = GetFilePath(folder, prefix, language);

            if (!File.Exists(filePath))
            {
                var errorMsg = $"Prompt file not found at '{filePath}'. " +
                    $"Expected location: Resources/Prompts/{folder}/{prefix}.{language}.json";
                _logger.LogError(errorMsg);
                throw new FileNotFoundException(errorMsg, filePath);
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                using var doc = JsonDocument.Parse(jsonContent);

                if (!doc.RootElement.TryGetProperty("prompt", out var promptElement))
                {
                    var errorMsg = $"Prompt JSON file '{filePath}' is missing 'prompt' property. " +
                        "Expected format: {{ \"prompt\": \"your prompt text here\" }}";
                    _logger.LogError(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                if (promptElement.ValueKind != JsonValueKind.String)
                {
                    var errorMsg = $"Prompt property in '{filePath}' is not a string. " +
                        $"Got: {promptElement.ValueKind}";
                    _logger.LogError(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                var promptText = promptElement.GetString();

                if (string.IsNullOrWhiteSpace(promptText))
                {
                    var errorMsg = $"Prompt text in file '{filePath}' is empty or whitespace only.";
                    _logger.LogError(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                _logger.LogInformation(
                    "Successfully loaded prompt from '{FilePath}' ({CharCount} characters)",
                    filePath, promptText.Length);

                return promptText;
            }
            catch (JsonException ex)
            {
                var errorMsg = $"Invalid JSON structure in prompt file '{filePath}'. " +
                    "Expected format: {{ \"prompt\": \"your text\" }}";
                _logger.LogError(ex, errorMsg);
                throw new InvalidDataException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = $"IO error reading prompt file '{filePath}'.";
                _logger.LogError(ex, errorMsg);
                throw new IOException(errorMsg, ex);
            }
        }

        private string GetFilePath(string folder, string prefix, string language)
        {
            return Path.Combine(_basePath, folder, $"{prefix}.{language}.json");
        }

        private async Task<UserAIProfile?> GetUserAIProfileAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            try
            {
                return await _context.UserAIProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load UserAIProfile for User {UserId}", userId);
                throw;
            }
        }
    }
}
