using HydroEspinaca.Shared.Extensions;
using SensorService.Application.Validators.Sensor;
using SensorService.Api.Services;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;

namespace SensorService.Api.Extensions;

/// <summary>
/// Extension methods for registering web-layer services (CORS, JSON serialization, Swagger).
/// </summary>
public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddSensorServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ✅ One line for all standard configuration
        services.AddHydroEspinacaMicroservice(
            configuration,
            "sensor-service",           // Service name for logs
            "Sensor Service API",       // Title for Swagger
            typeof(SensorCreateValidator).Assembly  // Application layer assembly
        );

        // ✅ Service-specific exception mapper
        services.AddScoped<IExceptionToProblemDetailsMapper, SensorServiceExceptionMapper>();
        
        return services;
    }
}