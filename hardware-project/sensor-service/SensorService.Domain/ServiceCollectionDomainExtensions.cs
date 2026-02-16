using Microsoft.Extensions.DependencyInjection;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Services;

namespace SensorService.Domain;

/// <summary>
/// Extensiones para el contenedor de inyección de dependencias que registran
/// los servicios de la capa de dominio del servicio de sensores.
/// </summary>
public static class ServiceCollectionDomainExtensions
{
    /// <summary>
    /// Registra todos los servicios de dominio en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor.</param>
    /// <returns>La colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAlertCalculationService, AlertCalculationService>();
        services.AddScoped<IAggregationService, AggregationService>();
        services.AddScoped<IAlertResolutionService, AlertResolutionService>();
        services.AddScoped<ICriticalReadingEvaluationService, CriticalReadingEvaluationService>();
        services.AddScoped<IEsp32StatusService, Esp32StatusService>();

        return services;
    }
}
