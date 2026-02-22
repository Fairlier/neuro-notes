using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserAIProfile
{
    public class UpdateUserAIProfileCommandHandler : IRequestHandler<UpdateUserAIProfileCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly AIOptions _aiDefaults;
        private readonly ILogger<UpdateUserAIProfileCommandHandler> _logger; 

        public UpdateUserAIProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IOptions<AIOptions> aiDefaults,
            ILogger<UpdateUserAIProfileCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _aiDefaults = aiDefaults.Value;
            _logger = logger;
        }

        public async Task Handle(UpdateUserAIProfileCommand request, CancellationToken token)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt in UpdateUserAIProfile.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            _logger.LogInformation(
                "Starts updating AI profile for User {UserId}.", 
                userId);

            var userAIProfile = await _context.UserAIProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, token);

            if (userAIProfile is null)
            {
                _logger.LogInformation(
                    "User AI profile not found. Creating new profile for User {UserId}.", 
                    userId);
                userAIProfile = new UserAIProfile(userId);
                await _context.UserAIProfiles.AddAsync(userAIProfile, token);
            }

            var targetLanguage = ResolveString(
                request.AIOperationLanguage,
                userAIProfile.AIOperationLanguage,
                _aiDefaults.DefaultAIOperationLanguage);

            var transcriptionProvider = ResolveProvider(
                request.TranscriptionProvider,
                userAIProfile.TranscriptionProvider,
                _aiDefaults.DefaultTranscriptionProvider);

            var structureProvider = ResolveProvider(
                request.StructureProvider,
                userAIProfile.StructureProvider,
                _aiDefaults.DefaultStructureProvider);

            var summaryProvider = ResolveProvider(
                request.SummaryProvider,
                userAIProfile.SummaryProvider,
                _aiDefaults.DefaultSummaryProvider);

            var globalChatProvider = ResolveProvider(
                request.GlobalChatProvider,
                userAIProfile.GlobalChatProvider,
                _aiDefaults.DefaultGlobalChatProvider);

            var noteChatProvider = ResolveProvider(
                request.NoteChatProvider,
                userAIProfile.NoteChatProvider,
                _aiDefaults.DefaultNoteChatProvider);

            userAIProfile.UpdatePreferences(
                targetLanguage,
                transcriptionProvider,
                structureProvider,
                summaryProvider,
                globalChatProvider,
                noteChatProvider
            );

            if (request.ProviderSettings is not null)
            {
                foreach (var kvp in request.ProviderSettings)
                {
                    userAIProfile.UpdateProviderSettings(kvp.Key, kvp.Value);
                }
            }

            if (request.Transcription is not null)
            {
                userAIProfile.UpdateTranscription(
                    request.Transcription.TargetLanguage ?? userAIProfile.Transcription.TargetLanguage,
                    request.Transcription.CustomPrompt ?? userAIProfile.Transcription.CustomPrompt,
                    request.Transcription.UseCustomPrompt ?? userAIProfile.Transcription.UseCustomPrompt
                );
            }

            if (request.Structuring is not null)
            {
                userAIProfile.UpdateStructuring(
                    request.Structuring.TargetLanguage ?? userAIProfile.Structuring.TargetLanguage,
                    request.Structuring.CustomPrompt ?? userAIProfile.Structuring.CustomPrompt,
                    request.Structuring.UseCustomPrompt ?? userAIProfile.Structuring.UseCustomPrompt
                );
            }

            if (request.Summarization is not null)
            {
                userAIProfile.UpdateSummarization(
                    request.Summarization.TargetLanguage ?? userAIProfile.Summarization.TargetLanguage,
                    request.Summarization.CustomPrompt ?? userAIProfile.Summarization.CustomPrompt,
                    request.Summarization.UseCustomPrompt ?? userAIProfile.Summarization.UseCustomPrompt
                );
            }

            if (request.GlobalChat is not null)
            {
                userAIProfile.UpdateGlobalChat(
                    request.GlobalChat.TargetLanguage ?? userAIProfile.GlobalChat.TargetLanguage,
                    request.GlobalChat.CustomPrompt ?? userAIProfile.GlobalChat.CustomPrompt,
                    request.GlobalChat.UseCustomPrompt ?? userAIProfile.GlobalChat.UseCustomPrompt
                );
            }

            if (request.NoteChat is not null)
            {
                userAIProfile.UpdateNoteChat(
                    request.NoteChat.TargetLanguage ?? userAIProfile.NoteChat.TargetLanguage,
                    request.NoteChat.CustomPrompt ?? userAIProfile.NoteChat.CustomPrompt,
                    request.NoteChat.UseCustomPrompt ?? userAIProfile.NoteChat.UseCustomPrompt
                );
            }


            await _context.SaveChangesAsync(token);

            _logger.LogInformation(
                "User AI profile updated successfully for User {UserId}.", 
                userId);
        }

        private T ResolveProvider<T>(T? requestValue, T databaseValue, T defaultValue) 
            where T : struct, Enum
        {
            if (requestValue.HasValue) return requestValue.Value;

            if (Convert.ToInt32(databaseValue) != 0) return databaseValue;

            return defaultValue;
        }

        private static string ResolveString(string? requestValue, string databaseValue, string defaultValue)
        {
            if (!string.IsNullOrEmpty(requestValue)) return requestValue;

            if (!string.IsNullOrEmpty(databaseValue)) return databaseValue;
            
            return defaultValue;
        }
    }
}
