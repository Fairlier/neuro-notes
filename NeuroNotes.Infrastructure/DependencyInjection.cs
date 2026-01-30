
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Infrastructure.Extensions;

namespace NeuroNotes.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSystemInfrastructure(configuration);

            services.AddPersistenceInfrastructure(configuration);

            services.AddIdentityInfrastructure(configuration);

            services.AddFileInfrastructure(configuration);

            services.AddAIInfrastructure(configuration);

            return services;
        }
    }
}
