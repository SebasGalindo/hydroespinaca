using SensorService.Infrastructure.Services;

namespace SensorService.Api.Extensions;

public static class ServiceCollectionWorkerExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<AggregateWorker>();
        services.AddHostedService<SensorMqttWorker>();
        services.AddHostedService<Esp32StatusMqttWorker>();
        services.AddHostedService<AlertCleanupWorker>();
        
        return services;
    }
}
