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

        services.AddTransient<ExifToolWrapper>();
        services.AddSingleton<ExifWatcherWrapper>();

        return services;
    }
}
