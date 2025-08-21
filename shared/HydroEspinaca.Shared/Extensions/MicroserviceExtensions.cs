using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace HydroEspinaca.Shared.Extensions;

/// <summary>
/// Extension methods for common microservice configuration in HydroEspinaca
/// </summary>
public static class MicroserviceExtensions
{
    /// <summary>
    /// Adds all standard microservice configurations for HydroEspinaca
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="serviceName">Name of the microservice</param>
    /// <param name="apiTitle">API title for Swagger</param>
    /// <param name="validatorAssembly">Assembly containing FluentValidation validators (optional)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHydroEspinacaMicroservice(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        string serviceName,
        string apiTitle,
        Assembly? validatorAssembly = null)
    {
        // Log environment information
        LogEnvironmentInfo(serviceName);

        // Add standard web API services
        services.AddControllers();
        
        // Add authentication and authorization
        services.AddHydroEspinacaAuth(configuration, serviceName);
        
        // Add Swagger documentation
        services.AddHydroEspinacaSwagger(apiTitle);
        
        // Add FluentValidation if assembly provided
        if (validatorAssembly != null)
        {
            services.AddValidatorsFromAssembly(validatorAssembly);
        }
        
        // Add health checks
        services.AddHealthChecks();

        return services;
    }

    /// <summary>
    /// Adds standard web API services for HydroEspinaca microservices
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">Name of the microservice for logging</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStandardWebServices(
        this IServiceCollection services,
        string serviceName)
    {
        LogEnvironmentInfo(serviceName);
        
        services.AddControllers();
        services.AddHealthChecks();
        
        return services;
    }

    /// <summary>
    /// Adds FluentValidation for the specified assembly containing validators
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="validatorAssembly">Assembly containing FluentValidation validators</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFluentValidationFromAssembly(
        this IServiceCollection services,
        Assembly validatorAssembly)
    {
        services.AddValidatorsFromAssembly(validatorAssembly);
        return services;
    }

    /// <summary>
    /// Adds FluentValidation for the assembly containing the specified type
    /// </summary>
    /// <typeparam name="T">Type from the assembly containing validators</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFluentValidationFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<T>();
        return services;
    }

    /// <summary>
    /// Gets environment information for the current microservice
    /// </summary>
    /// <param name="serviceName">Name of the microservice</param>
    /// <returns>Environment information</returns>
    public static EnvironmentInfo GetEnvironmentInfo(string serviceName)
    {
        var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var hostEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        
        var environment = aspNetCoreEnv ?? hostEnv ?? "Unknown";
        var isTestEnvironment = environment == "Test";
        var isDevelopment = environment == "Development";
        var isProduction = environment == "Production";

        return new EnvironmentInfo
        {
            ServiceName = serviceName,
            Environment = environment,
            IsTest = isTestEnvironment,
            IsDevelopment = isDevelopment,
            IsProduction = isProduction
        };
    }

    /// <summary>
    /// Logs environment information for the microservice
    /// </summary>
    /// <param name="serviceName">Name of the microservice</param>
    private static void LogEnvironmentInfo(string serviceName)
    {
        var envInfo = GetEnvironmentInfo(serviceName);
        Console.WriteLine($"🚀 Starting {envInfo.ServiceName} in {envInfo.Environment} environment");
    }
}

/// <summary>
/// Environment information for a microservice
/// </summary>
public class EnvironmentInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsTest { get; set; }
    public bool IsDevelopment { get; set; }
    public bool IsProduction { get; set; }
}