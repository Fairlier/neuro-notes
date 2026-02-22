using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using NeuroNotes.Domain.Enums;
using NeuroNotes.Infrastructure.AI.Providers.Gemini;
using NeuroNotes.Infrastructure.AI.Providers.Mistral;
using NeuroNotes.Infrastructure.AI.Providers.OllamaLocal;
using NeuroNotes.Infrastructure.AI.Providers.VoskLocal;

namespace NeuroNotes.Infrastructure.AI.Providers
{
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AIOptions _options;
        private readonly ILogger<AIProviderFactory> _logger; 
        public AIProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<AIOptions> options,
            ILogger<AIProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        public ITranscriptionService GetTranscriptionService()
            => GetTranscriptionService(_options.DefaultTranscriptionProvider);

        public ITranscriptionService GetTranscriptionService(TranscriptionProviderType provider)
        {
            _logger.LogDebug(
                "Resolving Transcription Service for provider: {Provider}", 
                provider);

            return provider switch
            {
                TranscriptionProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiTranscriptionService>(),
                TranscriptionProviderType.Mistral => _serviceProvider.GetRequiredService<MistralTranscriptionService>(),
                TranscriptionProviderType.VoskLocal => _serviceProvider.GetRequiredService<VoskTranscriptionService>(),
                _ => throw CreateNotSupportedException("Transcription", provider.ToString())
            };
        }

        public IStructureService GetStructureService()
            => GetStructureService(_options.DefaultStructureProvider);

        public IStructureService GetStructureService(StructureProviderType provider)
        {
            _logger.LogDebug(
                "Resolving Structure Service for provider: {Provider}", 
                provider);

            return provider switch
            {
                StructureProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiStructureService>(),
                StructureProviderType.OllamaLocal => _serviceProvider.GetRequiredService<OllamaLocalStructureService>(),
                StructureProviderType.Mistral => _serviceProvider.GetRequiredService<MistralStructureService>(),
                _ => throw CreateNotSupportedException("Structure", provider.ToString())
            };
        }


        public ISummaryService GetSummaryService()
            => GetSummaryService(_options.DefaultSummaryProvider);

        public ISummaryService GetSummaryService(SummaryProviderType provider)
        {
            _logger.LogDebug(
                "Resolving Summary Service for provider: {Provider}", 
                provider);

            return provider switch
            {
                SummaryProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiSummaryService>(),
                SummaryProviderType.OllamaLocal => _serviceProvider.GetRequiredService<OllamaLocalSummaryService>(),
                SummaryProviderType.Mistral => _serviceProvider.GetRequiredService<MistralSummaryService>(),
                _ => throw CreateNotSupportedException("Summary", provider.ToString())
            };
        }

        public IChatService GetGlobalChatService()
            => GetGlobalChatService(_options.DefaultGlobalChatProvider);

        public IChatService GetGlobalChatService(ChatProviderType provider)
        {
            _logger.LogDebug("Resolving Global Chat Service for provider: {Provider}", provider);

            return provider switch
            {
                ChatProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiChatService>(),
                ChatProviderType.OllamaLocal => _serviceProvider.GetRequiredService<OllamaLocalChatService>(),
                ChatProviderType.Mistral => _serviceProvider.GetRequiredService<MistralChatService>(),
                _ => throw CreateNotSupportedException("Global Chat", provider.ToString())
            };
        }

        public IChatService GetNoteChatService()
            => GetNoteChatService(_options.DefaultNoteChatProvider);

        public IChatService GetNoteChatService(ChatProviderType provider)
        {
            _logger.LogDebug("Resolving Note Chat Service for provider: {Provider}", provider);

            return provider switch
            {
                ChatProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiChatService>(),
                ChatProviderType.OllamaLocal => _serviceProvider.GetRequiredService<OllamaLocalChatService>(),
                ChatProviderType.Mistral => _serviceProvider.GetRequiredService<MistralChatService>(),
                _ => throw CreateNotSupportedException("Note Chat", provider.ToString())
            };
        }

        private NotSupportedException CreateNotSupportedException(string serviceType, string providerName)
        {
            var ex = new NotSupportedException(
                $"{serviceType} provider '{providerName}' is not supported or implemented.");
            _logger.LogError(ex, "Failed to resolve {ServiceType} service for provider {ProviderName}", serviceType, providerName);
            return ex;
        }
    }
}
