using SensorService.Application.Interfaces.UseCases.AggregateWorker;
using SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Application.UseCases;
using SensorService.Application.UseCases.Esp32OfflineWorker;
using SensorService.Application.UseCases.Esp32Status;
using SensorService.Application.UseCases.ProcessReadingBatch;
using SensorService.Domain.Interfaces;

namespace SensorService.Api.Extensions;

public static class ServiceCollectionUseCaseExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IMatchReadingsWithSensorsUseCase, MatchReadingsWithSensorsUseCase>();
        services.AddScoped<IGenerateAlertsUseCase, GenerateAlertsUseCase>();
        services.AddScoped<IGenerateInactiveSensorAlertsUseCase, GenerateInactiveSensorAlertsUseCase>();

        services.AddScoped<IProcessReadingBatchUseCase, ProcessReadingBatchUseCase>();
        services.AddScoped<IProcessAggregatesUseCase, ProcessAggregatesUseCase>();
        services.AddScoped<IHandleEsp32StatusUseCase, HandleEsp32StatusUseCase>();
        services.AddScoped<ICheckEsp32OfflineStatusUseCase, CheckEsp32OfflineStatusUseCase>();
        services.AddScoped<IMqttMessageHandler, MqttMessageHandler>();
        return services;
    }
}
