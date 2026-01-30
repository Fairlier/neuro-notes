using MediatR;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommand : IRequest
    {
        public string? Nickname { get; set; }
        public string? InterfaceLanguage { get; set; }
    }
}
