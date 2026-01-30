using MediatR;

namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public record GetSystemConfigQuery : IRequest<SystemConfigResponse>;
}
