using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Providers.Services;
using System.Text.Json;
using Vosk;

namespace NeuroNotes.Infrastructure.AI.Providers.VoskLocal
{
    public class VoskTranscriptionService : ITranscriptionService
    {
        private readonly VoskLocalOptions _options;
        private readonly ILogger<VoskTranscriptionService> _logger;
        private readonly RecyclableMemoryStreamManager _streamManager;
        private readonly IMemoryCache _cache;

        public VoskTranscriptionService(
            IOptions<VoskLocalOptions> options,
            ILogger<VoskTranscriptionService> logger,
            RecyclableMemoryStreamManager streamManager,
            IMemoryCache cache)
        {
            _options = options.Value;
            _logger = logger;
            _streamManager = streamManager;
            _cache = cache;

            Vosk.Vosk.SetLogLevel(-1);
        }

        public async Task<string> TranscribeAsync(
            Stream audioStream,
            string fileName,
            string contentType,
            string systemPrompt,
            Dictionary<string, string>? providerSettings,
            CancellationToken cancellationToken)
        {
            string modelPath;
            try
            {
                modelPath = ResolveModelPath(providerSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve Vosk model path.");
                throw;
            }

            var modelName = Path.GetFileName(modelPath);
            _logger.LogInformation("Starting Vosk processing for '{FileName}'. Model: {ModelName}", fileName, modelName);

            var transcriptionModel = _cache.GetOrCreate(modelPath, entry =>
            {
                _logger.LogInformation("Loading Vosk model '{ModelName}' into memory (Cold Start).", modelName);

                if (!Directory.Exists(modelPath))
                {
                    throw new DirectoryNotFoundException($"Vosk model not found at: {modelPath}");
                }

                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                entry.SetPriority(CacheItemPriority.High);
                entry.RegisterPostEvictionCallback(OnModelEvicted);

                return new Model(modelPath);
            });

            if (transcriptionModel is null) throw new InvalidOperationException("Failed to load Vosk model.");

            using var outputStream = _streamManager.GetStream("VoskResample");

            try
            {
                if (audioStream.CanSeek) audioStream.Position = 0;

                _logger.LogDebug("Converting audio to WAV 16kHz Mono.");

                await FFMpegArguments
                    .FromPipeInput(new StreamPipeSource(audioStream))
                    .OutputToPipe(new StreamPipeSink(outputStream), options => options
                        .WithAudioSamplingRate(16000)
                        .WithAudioCodec("pcm_s16le")
                        .WithCustomArgument("-ac 1")
                        .ForceFormat("wav"))
                    .ProcessAsynchronously();

                outputStream.Position = 0;

                return await Task.Run(() =>
                {
                    using var rec = new VoskRecognizer(transcriptionModel, 16000.0f);
                    rec.SetMaxAlternatives(0);
                    rec.SetWords(true);

                    long totalBytes = outputStream.Length;
                    long bytesReadTotal = 0;
                    byte[] buf = new byte[4096];
                    int read;
                    int lastProgress = 0;

                    while ((read = outputStream.Read(buf, 0, buf.Length)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rec.AcceptWaveform(buf, read);

                        bytesReadTotal += read;
                        if (totalBytes > 0)
                        {
                            int progress = (int)((double)bytesReadTotal / totalBytes * 100);
                            if (progress >= lastProgress + 20)
                            {
                                _logger.LogDebug("Vosk Progress: {Progress}%", progress);
                                lastProgress = progress;
                            }
                        }
                    }

                    _logger.LogInformation("Vosk recognition completed.");

                    var finalJson = rec.FinalResult();
                    using var doc = JsonDocument.Parse(finalJson);
                    return doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";

                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Vosk processing.");
                throw;
            }
        }

        private void OnModelEvicted(object key, object? value, EvictionReason reason, object? state)
        {
            if (value is IDisposable disposableModel)
            {
                disposableModel.Dispose();
                _logger.LogInformation("Vosk model '{ModelPath}' unloaded from memory. Reason: {Reason}", key, reason);
            }
        }

        private string ResolveModelPath(Dictionary<string, string>? config)
        {
            if (config is not null && config.TryGetValue("ModelsPath", out var customPath) && !string.IsNullOrWhiteSpace(customPath))
            {
                return customPath;
            }

            string? modelName;
            string languageKey = "en";

            if (config is not null && config.TryGetValue("Language", out var rawLang) && !string.IsNullOrWhiteSpace(rawLang))
            {
                int dashIndex = rawLang.IndexOf('-');
                languageKey = (dashIndex > 0 ? rawLang[..dashIndex] : rawLang).Trim().ToLowerInvariant();
            }

            if (!_options.LanguageMap.TryGetValue(languageKey, out modelName) &&
                !_options.LanguageMap.TryGetValue("en", out modelName))
            {
                throw new InvalidOperationException($"Vosk Config Error: Neither requested language '{languageKey}' nor default 'en' found in LanguageMap.");
            }

            return Path.Combine(_options.ModelsPath, modelName!);
        }
    }
}
