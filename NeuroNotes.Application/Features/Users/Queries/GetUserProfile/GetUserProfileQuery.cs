using MediatR;

namespace NeuroNotes.Application.Features.Users.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<UserProfileResponse>;
}
