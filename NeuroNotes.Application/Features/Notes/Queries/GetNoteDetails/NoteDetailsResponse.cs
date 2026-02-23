using AutoMapper;
using NeuroNotes.Application.Common.Mappings;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteDetails
{
    public class NoteDetailsResponse : IMapWith<Note>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string? Category { get; set; }

        public string? RawText { get; set; }
        public string? StructuredText { get; set; }
        public string? SummaryText { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Note, NoteDetailsResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.SourceType.ToString()))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category != null ? src.Category.ToString() : null));
        }
    }
}
