using Microsoft.Extensions.DependencyInjection;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Services;

namespace SensorService.Domain;

public static class ServiceCollectionDomainExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAlertCalculationService, AlertCalculationService>();
        services.AddScoped<IAggregationService, AggregationService>();
        services.AddScoped<IEsp32StatusService, Esp32StatusService>();

        return services;
    }
}
