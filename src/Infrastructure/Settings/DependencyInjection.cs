using Application.Infrastructure.Services;
using Infrastructure.Services;
using Infrastructure.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddTransient<IExifService, ExifService>();

        services.AddTransient<ExifToolWrapper>();
        services.AddSingleton<ExifWatcherWrapper>();

        return services;
    }
}
