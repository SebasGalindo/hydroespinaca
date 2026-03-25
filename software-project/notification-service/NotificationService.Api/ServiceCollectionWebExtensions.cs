using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using NotificationService.Api.Services;
using NotificationService.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;
using FluentValidation;

namespace NotificationService.Api.Extensions;

/// <summary>
/// Extension methods for configuring services in the Notification Service API.
/// This class provides a centralized place to add and configure services specific to the API layer,
/// such as controllers, authentication, and exception handling.
/// </summary>
public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddNotificationServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Register the microservice with HydroEspinaca, including API documentation and validation
        services.AddHydroEspinacaMicroservice(
            configuration,
            serviceName: "notification-service",
            apiTitle: "Notification Service API",
            validatorAssembly: typeof(CreateNotificationGroupCommandValidator).Assembly
        );

        // Register the custom exception mapper for the notification service, which will handle specific exceptions and map them to appropriate HTTP responses
        services.AddSingleton<ProblemDetailsFactory>();
        services.AddSingleton<IExceptionToProblemDetailsMapper, NotificationServiceExceptionMapper>();

        return services;
    }
}
