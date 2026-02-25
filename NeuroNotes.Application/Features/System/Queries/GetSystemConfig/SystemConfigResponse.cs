
namespace NeuroNotes.Application.Features.System.Queries.GetSystemConfig
{
    public class SystemConfigResponse
    {
        public AudioConfigDto Audio { get; set; } = new();
        public ImageConfigDto Image { get; set; } = new();
        public ProvidersConfigDto Providers { get; set; } = new();
    }
}
