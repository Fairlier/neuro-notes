using MediatR;

namespace NeuroNotes.Application.Features.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommand : IRequest<UploadAvatarDto>
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
