using NeuroNotes.Application.Common.Mappings;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Features.Chat.Queries.GetChatHistory
{
    public class ChatMessageDto : IMapWith<ChatMessage>
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public void Mapping(AutoMapper.Profile profile)
        {
            profile.CreateMap<ChatMessage, ChatMessageDto>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));
        }
    }
}
