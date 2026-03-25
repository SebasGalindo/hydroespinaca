using ChatbotService.Api.Services;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Extensions;

namespace ChatbotService.Api.Extensions;

/// <summary>
/// Extensiones para configurar el API layer del chatbot-service
/// siguiendo el patrón estándar de los demás microservicios.
/// </summary>
public static class ServiceCollectionWebExtensions
{
    /// <summary>
    /// Agrega las configuraciones del API layer: Auth JWT, Swagger, validadores, health checks.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>Service collection actualizado.</returns>
    public static IServiceCollection AddChatbotServiceApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración estándar: Auth JWT, Swagger, CORS, Controllers
        services.AddHydroEspinacaMicroservice(
            configuration,
            serviceName: "chatbot-service",
            apiTitle: "Chatbot Service API",
            validatorAssembly: typeof(Application.DependencyInjection).Assembly
        );

        // Exception mapper específico del servicio
        services.AddScoped<IExceptionToProblemDetailsMapper, ChatbotServiceExceptionMapper>();

        // Memory cache para embeddings
        services.AddMemoryCache();

        return services;
    }
}
