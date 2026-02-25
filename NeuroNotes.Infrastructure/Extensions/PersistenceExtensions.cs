
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.BackgroundJobs;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Infrastructure.BackgroundJobs;
using NeuroNotes.Infrastructure.Persistence;

namespace NeuroNotes.Infrastructure.Extensions
{
    public static class PersistenceExtensions
    {
        public static IServiceCollection AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(NeuroNotesDbContext.ConnectionStringName);

            services.AddDbContext<NeuroNotesDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<NeuroNotesDbContext>());

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

            services.AddHangfireServer();
            services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            services.AddTransient<NoteProcessingSweeperJob>();

            services.AddTransient<OrphanedFilesCleanupJob>();

            return services;
        }
    }
}
