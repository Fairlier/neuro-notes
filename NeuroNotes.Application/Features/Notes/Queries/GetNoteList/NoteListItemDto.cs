using AutoMapper;
using NeuroNotes.Application.Common.Mappings;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class NoteListItemDto : IMapWith<Note>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Note, NoteListItemDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
