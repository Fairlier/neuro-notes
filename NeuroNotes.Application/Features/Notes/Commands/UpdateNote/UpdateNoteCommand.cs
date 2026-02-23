using MediatR;
using NeuroNotes.Domain.Enums;
using System.Text.Json.Serialization;

namespace NeuroNotes.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommand : IRequest
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? RawText { get; set; }
        public string? StructuredText { get; set; }
        public string? SummaryText { get; set; }
        public NoteCategory? Category { get; set; }
    }
}
