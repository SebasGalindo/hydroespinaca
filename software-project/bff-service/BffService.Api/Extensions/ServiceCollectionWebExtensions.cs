using HydroEspinaca.Shared.Extensions;
using BffService.Application.Validators;
using BffService.Api.Services;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;

namespace BffService.Api.Extensions;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddBffServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ✅ One line for all standard configuration
        services.AddHydroEspinacaMicroservice(
            configuration,
            "bff-service",                           // Service name for logs
            "BFF Service API",                       // Title for Swagger
            typeof(LogoutRequestValidator).Assembly   // Application layer assembly
        );

        // ✅ Service-specific exception mapper (registered in Program.cs to avoid conflicts)
        
        return services;
    }
}