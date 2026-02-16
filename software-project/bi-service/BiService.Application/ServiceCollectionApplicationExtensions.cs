using BiService.Application.Shared.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BiService.Application;

/// <summary>
/// Extensiones de IServiceCollection para registrar los servicios de la capa de aplicación del módulo BI.
/// </summary>
public static class ServiceCollectionApplicationExtensions
{
    /// <summary>
    /// Registra MediatR, FluentValidation y los pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Pipeline Behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
