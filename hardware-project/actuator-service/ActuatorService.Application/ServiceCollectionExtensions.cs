using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandService, RegisterCommandService>();
        services.AddScoped<IActuatorService, ActuatorServiceApplication>();

        return services;
    }
}
