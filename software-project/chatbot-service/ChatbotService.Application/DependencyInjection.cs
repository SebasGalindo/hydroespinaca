using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ChatbotService.Application.Behaviors;

namespace ChatbotService.Application;

/// <summary>
/// Registra las dependencias de Applicación (MediatR/Validaciones).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los pipelines a las inyecciones DI de .NET 9.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var currentAssembly = typeof(DependencyInjection).Assembly;
        
        // Registra los handlers (Commands y Queries) que implementan IRequest.
        services.AddMediatR(config => 
        {
            config.RegisterServicesFromAssembly(currentAssembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Validaciones Fluent.
        services.AddValidatorsFromAssembly(currentAssembly);
        
        return services;
    }
}
