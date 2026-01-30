using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using NeuroNotes.Api.Middleware; 
using NeuroNotes.Application;
using NeuroNotes.Infrastructure;
using NeuroNotes.Infrastructure.BackgroundJobs;
using NeuroNotes.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var allowedOriginsStr = builder.Configuration["AllowedCorsOrigins"];
var allowedOrigins = !string.IsNullOrEmpty(allowedOriginsStr)
    ? allowedOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray()
    : Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireClaim("Permission", "Permissions.Users.Manage"));

    options.AddPolicy("CanUseAdvancedModels", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NeuroNotes API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<NeuroNotesDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.InitializeAsync(context, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.UseMiddleware<CustomExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "NeuroNotes Jobs",
    Authorization = new[]
    {
        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
        {
            RequireSsl = false,
            SslRedirect = false,
            LoginCaseSensitive = true,
            Users = new []
            {
                new BasicAuthAuthorizationUser
                {
                    Login = builder.Configuration["Hangfire:User"] ?? "admin",
                    PasswordClear = builder.Configuration["Hangfire:Pass"] ?? "admin"
                }
            }
        })
    }
});

app.MapControllers();

RecurringJob.AddOrUpdate<NoteProcessingSweeperJob>(
    "note-processing-sweeper",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.MinuteInterval(5));

app.Run();