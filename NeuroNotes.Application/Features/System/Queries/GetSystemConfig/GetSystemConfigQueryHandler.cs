using MediatR;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Constants;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public class GetSystemConfigQueryHandler : IRequestHandler<GetSystemConfigQuery, SystemConfigResponse>
    {
        private readonly ILogger<GetSystemConfigQueryHandler> _logger;

        public GetSystemConfigQueryHandler(ILogger<GetSystemConfigQueryHandler> logger)
        {
            _logger = logger;
        }

        public Task<SystemConfigResponse> Handle(GetSystemConfigQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starts retrieving system configuration.");

            var response = new SystemConfigResponse
            {
                SupportedAudioExtensions = FileConstants.SupportedAudioExtensions.ToList(),

                TranscriptionProviders = Enum.GetNames(typeof(TranscriptionProviderType)).ToList(),
                ChatProviders = Enum.GetNames(typeof(ChatProviderType)).ToList(),
                StructureProviders = Enum.GetNames(typeof(StructureProviderType)).ToList(),
                SummaryProviders = Enum.GetNames(typeof(SummaryProviderType)).ToList()
            };

            _logger.LogInformation("System configuration retrieved successfully.");

            return Task.FromResult(response);
        }
    }
}
