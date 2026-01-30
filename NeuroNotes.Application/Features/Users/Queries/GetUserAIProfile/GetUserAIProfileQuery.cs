using MediatR;

namespace NeuroNotes.Application.Features.Users.Queries.GetUserAIProfile
{
    public record GetUserAIProfileQuery : IRequest<UserAIProfileResponse>;
}
