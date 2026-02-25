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
            _logger.LogInformation("Retrieving system configuration.");

            var response = new SystemConfigResponse
            {
                Audio = new AudioConfigDto
                {
                    SupportedExtensions = FileConstants.SupportedAudioExtensions.ToList(),
                    SupportedContentTypes = FileConstants.SupportedAudioContentTypes.ToList(),
                    MaxUploadSizeBytes = FileConstants.MaxAudioUploadSizeBytes,
                    MaxUploadSizeFormatted = FileConstants.MaxAudioUploadSizeMbString
                },
                Image = new ImageConfigDto
                {
                    SupportedExtensions = FileConstants.SupportedImageExtensions.ToList(),
                    SupportedContentTypes = FileConstants.SupportedImageContentTypes.ToList(),
                    MaxUploadSizeBytes = FileConstants.MaxImageUploadSizeBytes,
                    MaxUploadSizeFormatted = FileConstants.MaxImageUploadSizeMbString
                },
                Providers = new ProvidersConfigDto
                {
                    Transcription = Enum.GetNames(typeof(TranscriptionProviderType)).ToList(),
                    Structure = Enum.GetNames(typeof(StructureProviderType)).ToList(),
                    Summary = Enum.GetNames(typeof(SummaryProviderType)).ToList(),
                    Chat = Enum.GetNames(typeof(ChatProviderType)).ToList()
                }
            };

            _logger.LogInformation("System configuration retrieved.");

            return Task.FromResult(response);
        }
    }
}
