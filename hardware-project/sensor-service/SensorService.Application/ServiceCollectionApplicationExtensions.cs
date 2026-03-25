using Microsoft.Extensions.DependencyInjection;
using SensorService.Application.Interfaces;
using SensorService.Application.Interfaces.UseCases.Esp32;
using SensorService.Application.Services;
using SensorService.Application.UseCases.Esp32;
using SensorService.Application.UseCases.ProcessReadingBatch;

namespace SensorService.Application;

/// <summary>
/// Extensiones para la configuración de inyección de dependencias de la capa de aplicación.
/// Registra todos los servicios de aplicación y casos de uso del servicio de sensores.
/// </summary>
public static class ServiceCollectionApplicationExtensions
{
    /// <summary>
    /// Registra todos los servicios de aplicación y casos de uso en el contenedor de inyección de dependencias.
    /// Incluye servicios para sensores, lecturas, alertas, variables, agregados, nodos ESP32 y alertas críticas.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor DI.</param>
    /// <returns>La colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<ISensorService, SensorApplicationService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<ILatestReadingsService, LatestReadingsService>();
        services.AddScoped<ISensorAlertService, SensorAlertService>();
        services.AddScoped<IAggregateService, AggregateService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<IEsp32NodeService, Esp32NodeService>();
        services.AddScoped<IEsp32AlertService, Esp32AlertService>();
        services.AddScoped<IUpdateEsp32LastSeenUseCase , UpdateEsp32LastSeenUseCase>();
        services.AddScoped<ICriticalAlertApplicationService, CriticalAlertApplicationService>();

        // Use Cases
        services.AddScoped<MqttMessageDispatcher>();
        return services;
    }
}
