using ActuatorService.Api.Services;
using ActuatorService.Application.Configuration;
using ActuatorService.Application.Validators;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Extensions;

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
            typeof(ExecuteCommandsValidator).Assembly
        );

        // Safety rules configuration
        services.Configure<SafetyRulesConfiguration>(
            configuration.GetSection(SafetyRulesConfiguration.SectionName));

        // Advanced rules configuration
        services.Configure<AdvancedRulesConfiguration>(
            configuration.GetSection(AdvancedRulesConfiguration.SectionName));

        // Service-specific exception mapper
        services.AddScoped<IExceptionToProblemDetailsMapper, ActuatorServiceExceptionMapper>();
        return services;
    }
}