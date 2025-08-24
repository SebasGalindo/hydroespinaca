using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace HydroEspinaca.Shared.Extensions;

/// <summary>
/// Extension methods for configuring Swagger with JWT authentication for HydroEspinaca microservices
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Adds standard Swagger configuration with JWT Bearer authentication for HydroEspinaca microservices
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="apiTitle">The API title (e.g., "User Service API")</param>
    /// <param name="apiVersion">The API version (default: "v1")</param>
    /// <param name="apiDescription">Optional API description</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHydroEspinacaSwagger(
        this IServiceCollection services,
        string apiTitle,
        string apiVersion = "v1",
        string? apiDescription = null)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Generate standard scope documentation
            var scopeDocumentation = GenerateStandardScopeDocumentation();
            
            var description = !string.IsNullOrEmpty(apiDescription) 
                ? $"{apiDescription}\n\n{scopeDocumentation}"
                : $"HydroEspinaca microservice with scope-based authorization. Each endpoint requires specific scopes in the JWT token.\n\n{scopeDocumentation}";

            c.SwaggerDoc(apiVersion, new OpenApiInfo 
            { 
                Title = apiTitle, 
                Version = apiVersion,
                Description = description
            });

            // Configure JWT Bearer authentication
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Swagger will automatically add 'Bearer ' prefix to your token."
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            c.AddSecurityRequirement(securityRequirement);
            
            // Enable XML comments if available for the calling assembly
            TryIncludeXmlComments(c);
        });

        return services;
    }

    /// <summary>
    /// Adds Swagger configuration with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="apiTitle">The API title</param>
    /// <param name="configureSwagger">Additional swagger configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHydroEspinacaSwagger(
        this IServiceCollection services,
        string apiTitle,
        Action<SwaggerGenOptions> configureSwagger)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Apply standard configuration first
            var scopeDocumentation = GenerateStandardScopeDocumentation();
            
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = apiTitle, 
                Version = "v1",
                Description = $"HydroEspinaca microservice with scope-based authorization.\n\n{scopeDocumentation}"
            });

            ConfigureJwtAuthentication(c);
            TryIncludeXmlComments(c);
            
            // Apply custom configuration
            configureSwagger(c);
        });

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication for Swagger
    /// </summary>
    private static void ConfigureJwtAuthentication(SwaggerGenOptions c)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Swagger will automatically add 'Bearer ' prefix to your token."
        };

        c.AddSecurityDefinition("Bearer", securityScheme);

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        };

        c.AddSecurityRequirement(securityRequirement);
    }

    /// <summary>
    /// Tries to include XML comments for the calling assembly
    /// </summary>
    private static void TryIncludeXmlComments(SwaggerGenOptions c)
    {
        try
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var xmlFilename = $"{callingAssembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        }
        catch
        {
            // Ignore XML comments errors
        }
    }

    /// <summary>
    /// Generates standard scope documentation for all HydroEspinaca services
    /// </summary>
    private static string GenerateStandardScopeDocumentation()
    {
        return "## Authorization Scopes\n\n" +
               "This service uses OAuth 2.0 scope-based authorization. The following scope patterns are available:\n\n" +
               "- **User Management**: `user:read`, `user:create`, `user:update`, `user:delete`\n" +
               "- **Profile Management**: `profile:read`, `profile:update`\n" +
               "- **Role Management**: `role:read`, `role:create`, `role:update`, `role:delete`\n" +
               "- **Permission Management**: `permission:read`, `permission:create`, `permission:update`, `permission:delete`\n" +
               "- **IoT Hardware**: `sensor:read`, `sensor:write`, `actuator:read`, `actuator:control`, `esp32:read`, `esp32:write`, `esp32:control`\n" +
               "- **Data Management**: `variable:read`, `variable:write`, `alert:read`, `alert:write`, `alert:manage`\n" +
               "- **System Operations**: `system:admin`, `system:health`, `system:monitor`\n\n" +
               "**Note**: Users with `system:admin` scope automatically bypass all authorization requirements.\n\n" +
               "See `HydroEspinaca.Shared.Enums.AuthorizationScopes` for the complete list of available scopes.";
    }
}