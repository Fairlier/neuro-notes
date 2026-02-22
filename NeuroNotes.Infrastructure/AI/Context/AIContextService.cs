using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Interfaces.AI.Context;
using NeuroNotes.Domain.Entities;
using System.Text;

namespace NeuroNotes.Infrastructure.AI.Context
{
    public class AIContextService : IAIContextService
    {
        private readonly ILogger<AIContextService> _logger;

        private const int AvgCharsPerToken = 4;
        private const int MaxRawTextLength = 50000;

        public AIContextService(ILogger<AIContextService> logger)
        {
            _logger = logger;
        }

        public IEnumerable<ChatMessage> OptimizeHistory(IEnumerable<ChatMessage> fullHistory, int maxTokens = 10000)
        {
            if (fullHistory is null)
            {
                _logger.LogWarning("History optimization skipped: fullHistory is null.");
                return Enumerable.Empty<ChatMessage>();
            }

            var historyList = fullHistory.OrderBy(m => m.CreatedAt).ToList();
            var maxChars = maxTokens * AvgCharsPerToken;
            int currentChars = 0;

            var tempResult = new List<ChatMessage>();

            for (int i = historyList.Count - 1; i >= 0; i--)
            {
                var msg = historyList[i];
                var length = msg.Content?.Length ?? 0;

                if (currentChars + length > maxChars)
                {
                    break;
                }

                currentChars += length;
                tempResult.Add(msg);
            }

            tempResult.Reverse();

            _logger.LogDebug("History optimized. Original: {OriginalCount}, Processed: {ProcessedCount}, ApproxTokens: {Tokens}",
                historyList.Count, tempResult.Count, currentChars / AvgCharsPerToken);

            return tempResult;
        }

        public string BuildContextFromNote(Note note)
        {
            if (note is null)
            {
                _logger.LogError("Attempted to build context from a null Note.");
                return string.Empty;
            }

            var sb = new StringBuilder();

            sb.AppendLine($"<current_note id=\"{note.Id}\" date=\"{note.CreatedAt:yyyy-MM-dd}\">");

            sb.AppendLine($"<title>{note.Title}</title>");

            if (!string.IsNullOrWhiteSpace(note.SummaryText))
            {
                sb.AppendLine("<summary>");
                sb.AppendLine(note.SummaryText);
                sb.AppendLine("</summary>");
            }

            if (!string.IsNullOrWhiteSpace(note.StructuredText))
            {
                sb.AppendLine("<content>");
                sb.AppendLine(note.StructuredText);
                sb.AppendLine("</content>");
            }
            else if (!string.IsNullOrWhiteSpace(note.RawText))
            {
                sb.AppendLine("<raw_transcript>");
                var text = note.RawText.Length > MaxRawTextLength
                    ? string.Concat(note.RawText.AsSpan(0, MaxRawTextLength), "...")
                    : note.RawText;
                sb.AppendLine(text);
                sb.AppendLine("</raw_transcript>");
            }

            sb.AppendLine("</current_note>");

            _logger.LogDebug("Built context for Note ID: {NoteId}. Length: {Length}", note.Id, sb.Length);
            return sb.ToString();
        }

        public string BuildContextFromNotesList(IEnumerable<Note> notes) // TODO нужен ли?
        {
            if (notes is null)
            {
                _logger.LogWarning("Attempted to build context from a null notes list.");
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<user_notes_overview>");

            int count = 0;
            foreach (var note in notes)
            {
                sb.AppendLine($"<note id=\"{note.Id}\" date=\"{note.CreatedAt:yyyy-MM-dd}\">");
                sb.AppendLine($"<title>{note.Title}</title>");

                if (!string.IsNullOrWhiteSpace(note.SummaryText))
                {
                    sb.AppendLine($"<summary>{note.SummaryText}</summary>");
                }

                sb.AppendLine("</note>");
                count++;
            }

            sb.AppendLine("</user_notes_overview>");

            _logger.LogDebug("Built global context from {Count} notes. Total Length: {Length}", count, sb.Length);
            return sb.ToString();
        }
    }
}
