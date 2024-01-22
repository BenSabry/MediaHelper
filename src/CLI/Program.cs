using Application;
using Application.Services;
using CLI;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation;

using (var service = GetService<CoreService>())
    await service.RunAsync();

static T GetService<T>()
    => Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(config => config.Sources.Clear())
    .ConfigureServices(services =>
    {
        services.AddSettings();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddPresentation();
    })
    .Build()
    .Services
    .GetRequiredService<T>();
