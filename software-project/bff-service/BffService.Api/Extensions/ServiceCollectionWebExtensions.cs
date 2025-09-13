using HydroEspinaca.Shared.Extensions;
using BffService.Application.Validators;
using BffService.Api.Services;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using HydroEspinaca.Shared.Authentication.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BffService.Api.Extensions;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddBffServiceApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ❌ Don't use standard microservice configuration due to FallbackPolicy 
        // Instead, configure manually without global auth requirement
        
        // Add standard web API services
        services.AddControllers();
        services.AddHealthChecks();
        
        // Add authentication without FallbackPolicy
        services.AddHydroEspinacaAuthWithoutFallback(configuration, "bff-service");
        
        // Add Swagger documentation with custom headers
        services.AddHydroEspinacaSwagger("BFF Service API", c =>
        {
            // Add custom header parameters for BFF session management
            c.OperationFilter<SessionHeadersOperationFilter>();
        });
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(LogoutRequestValidator).Assembly);
        
        // Add M2M authentication services
        services.AddM2MAuthentication(configuration);

        return services;
    }
    
    private static IServiceCollection AddHydroEspinacaAuthWithoutFallback(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        // Add shared auth services without the fallback policy
        services.AddSharedAuthServices(
            configuration,
            configureJwt: options =>
            {
                options.Events.OnAuthenticationFailed = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("HydroEspinaca.Auth");
                    logger.LogWarning("JWT authentication failed in {ServiceName}: {Error}", 
                        serviceName, context.Exception.Message);
                    return Task.CompletedTask;
                };
            },
            configureAuthorization: options =>
            {
                // ✅ No FallbackPolicy - allow anonymous access by default
                // Controllers can use [Authorize] where needed
            });
            
        return services;
    }
}