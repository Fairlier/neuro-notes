using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.AI.Classification;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Infrastructure.AI.Classification
{
    public class NoteCategoryClassifier : INoteCategoryClassifier, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Dictionary<string, int> _vocab;
        private readonly CategoryClassifierOptions _options;
        private readonly ILogger<NoteCategoryClassifier> _logger;
        private readonly int _clsTokenId;
        private readonly int _sepTokenId;
        private readonly int _unkTokenId;
        private readonly int _padTokenId;
        private bool _disposed;

        private static readonly NoteCategory[] CategoryMapping = new[]
        {
            NoteCategory.Finance,
            NoteCategory.Ideas,
            NoteCategory.Personal,
            NoteCategory.Reference,
            NoteCategory.Study,
            NoteCategory.Work
        };

        private static readonly string[] CategoryNames =
            { "Finance", "Ideas", "Personal", "Reference", "Study", "Work" };

        public NoteCategoryClassifier(
            IOptions<CategoryClassifierOptions> options,
            ILogger<NoteCategoryClassifier> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.ModelPath))
                throw new InvalidOperationException("CategoryClassifier ModelPath is not configured.");

            if (!File.Exists(_options.ModelPath))
                throw new FileNotFoundException($"ONNX model not found at: {_options.ModelPath}");

            if (string.IsNullOrWhiteSpace(_options.VocabPath))
                throw new InvalidOperationException("CategoryClassifier VocabPath is not configured.");

            if (!File.Exists(_options.VocabPath))
                throw new FileNotFoundException($"Vocab file not found at: {_options.VocabPath}");

            _logger.LogInformation("Loading vocab from {Path}", _options.VocabPath);
            _vocab = LoadVocab(_options.VocabPath);

            _clsTokenId = _vocab.GetValueOrDefault("[CLS]", 101);
            _sepTokenId = _vocab.GetValueOrDefault("[SEP]", 102);
            _unkTokenId = _vocab.GetValueOrDefault("[UNK]", 100);
            _padTokenId = _vocab.GetValueOrDefault("[PAD]", 0);

            _logger.LogInformation(
                "Vocab loaded: {Count} tokens. Special tokens: CLS={CLS}, SEP={SEP}, UNK={UNK}, PAD={PAD}",
                _vocab.Count, _clsTokenId, _sepTokenId, _unkTokenId, _padTokenId);

            _logger.LogInformation("Loading model from {Path}", _options.ModelPath);
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            _session = new InferenceSession(_options.ModelPath, sessionOptions);

            var inputNames = string.Join(", ", _session.InputMetadata.Keys);
            var outputNames = string.Join(", ", _session.OutputMetadata.Keys);
            _logger.LogInformation("Model loaded. Inputs: [{Inputs}], Outputs: [{Outputs}]", inputNames, outputNames);
        }

        private static Dictionary<string, int> LoadVocab(string path)
        {
            var vocab = new Dictionary<string, int>();
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                vocab[lines[i]] = i;
            }
            return vocab;
        }

        public async Task<NoteCategory> ClassifyAsync(string text, CancellationToken cancellationToken = default)
        {
            var (category, _) = await ClassifyWithConfidenceAsync(text, cancellationToken);
            return category;
        }

        public Task<(NoteCategory Category, float Confidence)> ClassifyWithConfidenceAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Empty text provided, returning Other category.");
                return Task.FromResult((NoteCategory.Other, 0f));
            }

            try
            {
                var (inputIds, attentionMask) = Tokenize(text, _options.MaxSequenceLength);

                var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
                var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
                };

                using var results = _session.Run(inputs);
                var logits = results.First().AsEnumerable<float>().ToArray();
                var probabilities = Softmax(logits);

                LogAllProbabilities(text, probabilities);

                int maxIndex = 0;
                float maxProb = probabilities[0];
                for (int i = 1; i < probabilities.Length; i++)
                {
                    if (probabilities[i] > maxProb)
                    {
                        maxProb = probabilities[i];
                        maxIndex = i;
                    }
                }

                if (maxProb < _options.MinConfidenceThreshold)
                {
                    _logger.LogDebug(
                        "Confidence {Confidence:P1} below threshold {Threshold:P1}. Returning Other.",
                        maxProb, _options.MinConfidenceThreshold);
                    return Task.FromResult((NoteCategory.Other, maxProb));
                }

                if (maxIndex >= 0 && maxIndex < CategoryMapping.Length)
                {
                    var category = CategoryMapping[maxIndex];
                    _logger.LogDebug("Result: {Category} ({Confidence:P1})", category, maxProb);
                    return Task.FromResult((category, maxProb));
                }

                _logger.LogWarning("Invalid model output index: {Index}. Returning Other.", maxIndex);
                return Task.FromResult((NoteCategory.Other, 0f));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during classification. Returning Other.");
                return Task.FromResult((NoteCategory.Other, 0f));
            }
        }

        private void LogAllProbabilities(string text, float[] probabilities)
        {
            var truncatedText = text.Length > 50 ? text[..50] + "..." : text;

            var parts = new List<string>();
            for (int i = 0; i < probabilities.Length && i < CategoryNames.Length; i++)
            {
                parts.Add($"{CategoryNames[i]}={probabilities[i]:P0}");
            }

            _logger.LogInformation(
                "Classification: \"{Text}\" => [{Probabilities}]",
                truncatedText,
                string.Join(", ", parts));
        }

        private (long[] inputIds, long[] attentionMask) Tokenize(string text, int maxLength)
        {
            var inputIds = new long[maxLength];
            var attentionMask = new long[maxLength];

            var tokenIds = new List<int> { _clsTokenId };

            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var wordTokens = TokenizeWord(word);
                tokenIds.AddRange(wordTokens);

                if (tokenIds.Count >= maxLength - 1)
                    break;
            }

            tokenIds.Add(_sepTokenId);

            for (int i = 0; i < maxLength; i++)
            {
                if (i < tokenIds.Count)
                {
                    inputIds[i] = tokenIds[i];
                    attentionMask[i] = 1;
                }
                else
                {
                    inputIds[i] = _padTokenId;
                    attentionMask[i] = 0;
                }
            }

            return (inputIds, attentionMask);
        }

        private List<int> TokenizeWord(string word)
        {
            var tokens = new List<int>();
            var remaining = word;
            bool isFirstPiece = true;

            while (remaining.Length > 0)
            {
                string? foundToken = null;
                int foundLength = 0;

                for (int len = Math.Min(remaining.Length, 40); len > 0; len--)
                {
                    var piece = remaining[..len];
                    var candidate = isFirstPiece ? piece : "##" + piece;

                    if (_vocab.ContainsKey(candidate))
                    {
                        foundToken = candidate;
                        foundLength = len;
                        break;
                    }
                }

                if (foundToken != null)
                {
                    tokens.Add(_vocab[foundToken]);
                    remaining = remaining[foundLength..];
                    isFirstPiece = false;
                }
                else
                {
                    tokens.Add(_unkTokenId);
                    remaining = remaining.Length > 1 ? remaining[1..] : "";
                    isFirstPiece = false;
                }
            }

            return tokens;
        }

        private static float[] Softmax(float[] logits)
        {
            var maxLogit = logits.Max();
            var expValues = logits.Select(x => MathF.Exp(x - maxLogit)).ToArray();
            var sumExp = expValues.Sum();
            return expValues.Select(x => x / sumExp).ToArray();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }
}
