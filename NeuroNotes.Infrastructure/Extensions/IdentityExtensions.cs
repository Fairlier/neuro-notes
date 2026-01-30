
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Infrastructure.Identity.Models;
using NeuroNotes.Infrastructure.Identity.Services;
using NeuroNotes.Infrastructure.Identity.Tokens;
using NeuroNotes.Infrastructure.Persistence;
using System.Text;

namespace NeuroNotes.Infrastructure.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<NeuroNotesDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.AccessTokenSecret))
            {
                throw new InvalidOperationException("JwtSettings are not configured properly. Check your .env file or appsettings.json.");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.AccessTokenSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IIdentityService, IdentityService>();

            return services;
        }
    }
}
