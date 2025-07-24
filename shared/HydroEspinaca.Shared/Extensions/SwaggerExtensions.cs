using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace HydroEspinaca.Shared.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithApiKey(this IServiceCollection services, string title, string version)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });

            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key necesaria. Usa el header: X-API-Key",
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    },
                    In = ParameterLocation.Header
                },
                new List<string>()
            }
        });
        });

        return services;
    }

}
