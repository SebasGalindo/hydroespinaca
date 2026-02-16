using SensorService.Infrastructure.Services;

namespace SensorService.Api.Extensions;

/// <summary>
/// Extension methods for registering background worker hosted services.
/// </summary>
public static class ServiceCollectionWorkerExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<AggregateWorker>();
        services.AddHostedService<SensorMqttWorker>();
        services.AddHostedService<Esp32StatusMqttWorker>();
        services.AddHostedService<Esp32OfflineWorker>();
        services.AddHostedService<AlertCleanupWorker>();

        return services;
    }
}
