using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using NeuroNotes.Application.Common.Options;

namespace NeuroNotes.Infrastructure.Extensions
{
    public static class SystemExtensions
    {
        public static IServiceCollection AddSystemInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services.Configure<AppDefaultsOptions>(configuration.GetSection(AppDefaultsOptions.SectionName));
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddDataProtection();
            services.AddSingleton<RecyclableMemoryStreamManager>(); 

            return services;
        }
    }
}
