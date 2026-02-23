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

            Status = NoteStatus.Pending;
            IsProcessing = true;
            CreatedAt = DateTime.UtcNow;
        }

        public string UserId { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public NoteSourceType SourceType { get; private set; }
        public string? SourceFileUrl { get; private set; }

        public NoteCategory? Category { get; private set; }
        public string? ErrorMessage { get; private set; }

        public string? RawText { get; private set; }       
        public string? StructuredText { get; private set; } 
        public string? SummaryText { get; private set; }    

        public NoteStatus Status { get; private set; }

        public bool IsProcessing { get; private set; }

        public void StartProcessing()
        {
            IsProcessing = true;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void FinishProcessing()
        {
            IsProcessing = false;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void FailProcessing(string error)
        {
            Status = NoteStatus.Failed;
            IsProcessing = false;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = error;
        }

        public void SetCategory(NoteCategory? category)
        {
            Category = category;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRawText(string text)
        {
            RawText = text;
            Status = NoteStatus.Raw;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void SetStructuredText(string structured)
        {
            StructuredText = structured;
            Status = NoteStatus.Structured;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void SetSummaryText(string summary)
        {
            SummaryText = summary;
            Status = NoteStatus.Summarized;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = null;
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
                Status = NoteStatus.Summarized;
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
