using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Authentication.Interfaces;
using ActuatorService.Api.Services;
using ActuatorService.Application.Validators;

namespace ActuatorService.Api.Extensions;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddActuatorServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // One-line standard configuration
        services.AddHydroEspinacaMicroservice(
            configuration,
            "actuator-service",
            "Actuator Service API",
            typeof(ActuatorService.Application.Validators.Actuators.CreateActuatorValidator).Assembly
        );

        // Service-specific exception mapper
        services.AddScoped<IExceptionToProblemDetailsMapper, ActuatorServiceExceptionMapper>();
        
        return services;
    }
}