using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Application.Common.Behaviors;
using NeuroNotes.Application.Common.Mappings;
using System.Reflection;

namespace NeuroNotes.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddAutoMapper(config => config.AddProfile(new AssemblyMappingProfile(assembly)));

            services.AddValidatorsFromAssembly(assembly);

            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}
