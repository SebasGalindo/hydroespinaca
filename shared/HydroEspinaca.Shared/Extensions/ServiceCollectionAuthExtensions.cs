using HydroEspinaca.Shared.Authentication.Extensions;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HydroEspinaca.Shared.Extensions;

/// <summary>
/// Extension methods for adding complete shared authentication configuration
/// </summary>
public static class ServiceCollectionAuthExtensions
{
    /// <summary>
    /// Adds complete shared authentication and authorization configuration for microservices
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing JWT settings</param>
    /// <param name="serviceName">Name of the microservice (for logging)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMicroserviceAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        // Add shared auth services
        services.AddSharedAuthServices(
            configuration,
            configureJwt: options =>
            {
                // Microservice-specific JWT configuration can be added here
                options.Events.OnAuthenticationFailed = context =>
                {
                    // Log authentication failures with service name
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
                // Default authorization policies can be configured here
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

        return services;
    }

    /// <summary>
    /// Adds standard scope-based policies for HydroEspinaca microservices
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="additionalPolicies">Additional service-specific policies</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStandardScopePolicies(
        this IServiceCollection services,
        Dictionary<string, string[]>? additionalPolicies = null)
    {
        var standardPolicies = new Dictionary<string, string[]>
        {
            // Standard read policies
            ["UserRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead },
            ["ProfileRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead },
            ["SensorRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead },
            ["ActuatorRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorRead },
            ["VariableRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead },
            ["AlertRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertRead },
            ["NotificationRead"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationRead },
            
            // Standard write policies
            ["UserWrite"] = new[] { 
                HydroEspinaca.Shared.Enums.AuthorizationScopes.UserCreate,
                HydroEspinaca.Shared.Enums.AuthorizationScopes.UserUpdate,
                HydroEspinaca.Shared.Enums.AuthorizationScopes.UserDelete
            },
            ["SensorWrite"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorWrite },
            ["ActuatorControl"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorControl },
            ["VariableWrite"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableWrite },
            ["AlertWrite"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertWrite },
            ["NotificationSend"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend },
            ["NotificationManage"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationManage },
            ["NotificationFull"] = new[] { 
                HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationRead,
                HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationManage,
                HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationDiagnostics
            },
            
            // System policies
            ["SystemAdmin"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin },
            ["SystemHealth"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth },
            ["SystemMonitor"] = new[] { HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor }
        };

        // Merge with additional policies if provided
        if (additionalPolicies != null)
        {
            foreach (var policy in additionalPolicies)
            {
                standardPolicies[policy.Key] = policy.Value;
            }
        }

        services.AddScopePolicies(standardPolicies);
        
        // Also add individual scope policies for direct scope usage
        services.AddIndividualScopePolicies();
        
        return services;
    }

    /// <summary>
    /// Adds all standard microservice authentication configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing JWT settings</param>
    /// <param name="serviceName">Name of the microservice</param>
    /// <param name="additionalPolicies">Additional service-specific policies</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHydroEspinacaAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Dictionary<string, string[]>? additionalPolicies = null)
    {
        return services
            .AddMicroserviceAuth(configuration, serviceName)
            .AddStandardScopePolicies(additionalPolicies);
    }
}