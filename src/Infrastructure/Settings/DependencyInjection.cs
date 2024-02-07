using Application.Infrastructure.Services;
using Infrastructure.Services;
using Infrastructure.Wrappers;
using Microsoft.Extensions.DependencyInjection;
using Wrappers;

namespace Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerService, LoggerService>();

        //Exif
        services.AddSingleton<IExifCoreService, ExifBaseService>();
        services.AddTransient<IExifService, ExifService>();
        services.AddSingleton<ExifServiceSettings>();

        services.AddSingleton<ExifWatcherWrapper>();
        services.AddTransient((IServiceProvider provider) =>
        {
            var settings = provider.GetService<ExifServiceSettings>();
            ArgumentNullException.ThrowIfNull(settings);

            return new ExifToolWrapper(
                settings.ClearBackupFilesOnComplete,
                settings.AttemptToFixIncorrectOffsets,
                settings.IgnoreMinorErrorsAndWarnings);
        });

        return services;
    }
}
