using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
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

        private const string FallbackLanguage = "en"; 
        private const string DefaultAIResponse = "You are a helpful AI assistant.";

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
            => GetPromptInternalAsync(userId, "Transcription", "transcription", p => p.CustomTranscriptionPrompt);

        public Task<string> GetStructureSystemPromptAsync(string userId)
            => GetPromptInternalAsync(userId, "Structure", "structure", p => p.CustomStructurePrompt);

        public Task<string> GetSummarySystemPromptAsync(string userId)
            => GetPromptInternalAsync(userId, "Summary", "summary", p => p.CustomSummaryPrompt);

        public Task<string> GetChatSystemPromptAsync(string userId)
            => GetPromptInternalAsync(userId, "Chat", "chat", p => p.CustomChatPrompt);

        private async Task<string> GetPromptInternalAsync(
            string userId,
            string folderName,
            string fileNamePrefix,
            Func<UserAIProfile, string?> customPromptSelector)
        {
            _logger.LogDebug("Resolving prompt for '{Category}'. User: {UserId}", folderName, userId);

            var profile = await GetUserAIProfileAsync(userId);

            var customPrompt = profile != null ? customPromptSelector(profile) : null;
            if (!string.IsNullOrWhiteSpace(customPrompt))
            {
                _logger.LogDebug("Strategy: USER CUSTOM override applied for '{Category}'.", folderName);
                return customPrompt;
            }

            var targetLang = FallbackLanguage;

            if (profile != null && !string.IsNullOrWhiteSpace(profile.AIOperationLanguage))
            {
                targetLang = profile.AIOperationLanguage;
            }
            else if (!string.IsNullOrWhiteSpace(_aiOptions.DefaultAIOperationLanguage))
            {
                targetLang = _aiOptions.DefaultAIOperationLanguage;
            }

            targetLang = targetLang.Split('-')[0].ToLowerInvariant();

            return await GetCachedSystemPromptAsync(folderName, fileNamePrefix, targetLang);
        }

        private async Task<string> GetCachedSystemPromptAsync(string folder, string prefix, string lang)
        {
            var cacheKey = $"Prompt_{folder}_{prefix}_{lang}";

            var prompt = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                _logger.LogInformation("Cache MISS for prompt: {Key}. Loading from disk.", cacheKey);
                entry.SetOptions(_cacheEntryOptions);
                return await LoadSystemPromptFromDiskAsync(folder, prefix, lang);
            });

            return prompt ?? DefaultAIResponse;
        }

        private async Task<string> LoadSystemPromptFromDiskAsync(string folder, string prefix, string requestedLang)
        {
            var filePath = GetFilePath(folder, prefix, requestedLang);

            if (!File.Exists(filePath))
            {
                if (requestedLang == FallbackLanguage)
                {
                    _logger.LogError("Default prompt file missing at '{Path}'", filePath);
                    return DefaultAIResponse;
                }

                _logger.LogWarning("Localization file missing: '{Path}'. Switching to fallback language '{Fallback}'.",
                    filePath, FallbackLanguage);

                filePath = GetFilePath(folder, prefix, FallbackLanguage);

                if (!File.Exists(filePath))
                {
                    _logger.LogError("Both requested and default prompt files are missing. Last attempt: '{Path}'", filePath);
                    return DefaultAIResponse;
                }
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                using var doc = JsonDocument.Parse(jsonContent);

                if (doc.RootElement.TryGetProperty("prompt", out var promptEl) &&
                    promptEl.ValueKind == JsonValueKind.String)
                {
                    var text = promptEl.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }

                _logger.LogWarning("Prompt file '{Path}' is valid JSON but 'prompt' property is empty.", filePath);
                return DefaultAIResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON structure in file: '{Path}'", filePath);
                return DefaultAIResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading prompt file: '{Path}'", filePath);
                return DefaultAIResponse;
            }
        }

        private string GetFilePath(string folder, string prefix, string lang)
        {
            return Path.Combine(_basePath, folder, $"{prefix}.{lang}.json");
        }

        private async Task<UserAIProfile?> GetUserAIProfileAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.UserAIProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
    }
}
