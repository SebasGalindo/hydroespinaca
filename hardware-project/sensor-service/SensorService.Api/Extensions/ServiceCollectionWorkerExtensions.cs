using SensorService.Infrastructure.Services;

namespace SensorService.Api.Extensions;

public static class ServiceCollectionWorkerExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<AggregateWorker>();
        services.AddHostedService<Esp32OfflineWorker>();
        services.AddHostedService<SensorMqttWorker>();
        
        return services;
    }
}
