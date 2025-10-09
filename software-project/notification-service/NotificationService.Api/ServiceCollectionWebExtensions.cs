using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using NotificationService.Api.Services;
using NotificationService.Application.Validators;
using FluentValidation;

namespace NotificationService.Api.Extensions;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddNotificationServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // ✅ Usar configuración estándar de microservicio
        services.AddHydroEspinacaMicroservice(
            configuration,
            serviceName: "notification-service",
            apiTitle: "Notification Service API",
            validatorAssembly: typeof(SendEmailRequestValidator).Assembly
        );

        // ✅ Registrar el exception mapper específico del servicio
        services.AddSingleton<ProblemDetailsFactory>();
        services.AddSingleton<IExceptionToProblemDetailsMapper, NotificationServiceExceptionMapper>();

        return services;
    }
}
