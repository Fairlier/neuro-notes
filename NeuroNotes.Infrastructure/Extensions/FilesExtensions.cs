
using FileSignatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using NeuroNotes.Application.Common.Options;
using NeuroNotes.Application.Interfaces.Files;
using NeuroNotes.Infrastructure.BackgroundJobs;
using NeuroNotes.Infrastructure.Files.Formats;
using NeuroNotes.Infrastructure.Files.Storage;
using NeuroNotes.Infrastructure.Files.Validation;

namespace NeuroNotes.Infrastructure.Extensions
{
    public static class FilesExtensions
    {
        public static IServiceCollection AddFileInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
            services.AddScoped<IFileStorageService, MinioFileStorageService>();

            var recognisedFormats = new FileFormat[]
            {
                new Mp3Id3(),
                new Mp3Mpeg(),
                new Wav(),
                new Ogg(),
                new Flac(),
                new M4a(),
                new Jpeg(),
                new Png(),
                new Gif(),
                new WebP()
            };
            
            services.AddSingleton<IFileFormatInspector>(new FileFormatInspector(recognisedFormats));
            services.AddScoped<IFileSignatureValidator, FileSignatureValidator>();
            services.AddScoped<IMimeTypeDetector, FileSignaturesMimeTypeDetector>();

            return services;
        }
    }
}
