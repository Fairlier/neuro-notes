using NeuroNotes.Domain.Common;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Domain.Entities
{
    public class Note : BaseEntity
    {
        protected Note() { }

        public Note(string title, string userId, NoteSourceType sourceType, string? sourceFileUrl = null)
        {
            Title = title;
            UserId = userId;
            SourceType = sourceType;
            SourceFileUrl = sourceFileUrl;

            Status = sourceType == NoteSourceType.AudioFile ? NoteStatus.Processing : NoteStatus.Raw;
            CreatedAt = DateTime.UtcNow;
        }

        public string UserId { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public NoteSourceType SourceType { get; private set; }
        public string? SourceFileUrl { get; private set; }

        public string? RawText { get; private set; }       
        public string? StructuredText { get; private set; } 
        public string? SummaryText { get; private set; }    

        public NoteStatus Status { get; private set; }


        public void SetRawText(string text)
        {
            RawText = text;
            Status = NoteStatus.Raw;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStructuredText(string structured)
        {
            StructuredText = structured;
            Status = NoteStatus.Structured;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSummaryText(string summary)
        {
            SummaryText = summary;
            Status = NoteStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void FailProcessing(string error)
        {
            Status = NoteStatus.Failed;
            SummaryText = $"Error: {error}";
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateTitle(string title)
        {
            if (Title != title)
            {
                Title = title;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateRawText(string newText)
        {
            if (RawText != newText)
            {
                RawText = newText;
                Status = NoteStatus.Raw;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateStructuredText(string newText)
        {
            if (StructuredText != newText)
            {
                StructuredText = newText;
                Status = NoteStatus.Structured;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateSummaryText(string newText)
        {
            if (SummaryText != newText)
            {
                SummaryText = newText;
                Status = NoteStatus.Completed;
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
