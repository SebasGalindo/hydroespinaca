using Microsoft.Extensions.DependencyInjection;
using SensorService.Application.Interfaces;
using SensorService.Application.Services;
using SensorService.Application.UseCases.ProcessReadingBatch;

namespace SensorService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<ISensorService, SensorApplicationService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<ISensorAlertService, SensorAlertService>();
        services.AddScoped<IAggregateService, AggregateService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<IEsp32NodeService, Esp32NodeService>();
        services.AddScoped<IEsp32AlertService, Esp32AlertService>();

        // Use Cases
        services.AddScoped<MqttMessageDispatcher>();
        return services;
    }
}
