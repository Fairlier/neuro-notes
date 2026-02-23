
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Application.Interfaces.AI.Context;
using NeuroNotes.Application.Interfaces.AI.Embeddings;
using NeuroNotes.Application.Interfaces.AI.Prompting;
using NeuroNotes.Application.Interfaces.AI.Providers;
using NeuroNotes.Infrastructure.AI.Classification;
using NeuroNotes.Infrastructure.AI.Context;
using NeuroNotes.Infrastructure.AI.Embeddings;
using NeuroNotes.Infrastructure.AI.Prompting;
using NeuroNotes.Infrastructure.AI.Providers;
using NeuroNotes.Infrastructure.AI.Providers.Gemini;
using NeuroNotes.Infrastructure.AI.Providers.Mistral;
using NeuroNotes.Infrastructure.AI.Providers.OllamaLocal;
using NeuroNotes.Infrastructure.AI.Providers.VoskLocal;
using Polly;
using Polly.Extensions.Http;

namespace NeuroNotes.Infrastructure.Extensions
{
    public static class AIExtensions
    {
        public static IServiceCollection AddAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AIOptions>(configuration.GetSection(AIOptions.SectionName));
            services.Configure<GeminiOptions>(configuration.GetSection($"{AIOptions.SectionName}:Gemini"));
            services.Configure<MistralOptions>(configuration.GetSection($"{AIOptions.SectionName}:Mistral"));
            services.Configure<OllamaLocalOptions>(configuration.GetSection($"{AIOptions.SectionName}:OllamaLocal"));
            services.Configure<VoskLocalOptions>(configuration.GetSection($"{AIOptions.SectionName}:VoskLocal"));
            services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            services.AddHttpClient<GeminiTranscriptionService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<GeminiStructureService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<GeminiSummaryService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<GeminiChatService>().AddPolicyHandler(retryPolicy);

            services.AddHttpClient<MistralTranscriptionService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<MistralStructureService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<MistralSummaryService>().AddPolicyHandler(retryPolicy);
            services.AddHttpClient<MistralChatService>().AddPolicyHandler(retryPolicy);

            var localTimeout = TimeSpan.FromMinutes(10);
            services.AddHttpClient<OllamaLocalStructureService>(client => client.Timeout = localTimeout)
                .AddPolicyHandler(retryPolicy);
            services.AddHttpClient<OllamaLocalSummaryService>(client => client.Timeout = localTimeout)
                .AddPolicyHandler(retryPolicy);
            services.AddHttpClient<OllamaLocalChatService>(client => client.Timeout = localTimeout)
                .AddPolicyHandler(retryPolicy);

            services.AddHttpClient<GemmaLocalEmbeddingService>().AddPolicyHandler(retryPolicy);

            services.AddScoped<VoskTranscriptionService>();

            services.AddScoped<IAIContextService, AIContextService>();
            services.AddScoped<IPromptService, PromptService>();

            services.AddScoped<IAIProviderFactory, AIProviderFactory>();

            services.AddScoped<IEmbeddingModelFactory, EmbeddingModelFactory>();
            services.AddScoped<IEmbeddingService>(sp =>
                sp.GetRequiredService<IEmbeddingModelFactory>().Create());

            services.AddScoped<INoteEmbeddingGenerator, NoteEmbeddingGenerator>();

            services.AddScoped<IRagService, RagService>();

            services.Configure<CategoryClassifierOptions>(
                configuration.GetSection(CategoryClassifierOptions.SectionName));

            services.AddSingleton<INoteCategoryClassifier, NoteCategoryClassifier>();

            return services;
        }
    }
}
