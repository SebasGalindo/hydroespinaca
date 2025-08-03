using AuthService.Application.Validators;
using FluentValidation;
using Microsoft.OpenApi.Models;

namespace AuthService.Web;
public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                {
                    new OpenApiSecurityScheme{ Reference = new OpenApiReference{
                        Type=ReferenceType.SecurityScheme, Id="Bearer"
                    }},
                    Array.Empty<string>()
                }
            });
        });
        services.AddValidatorsFromAssemblyContaining<ClientCredentialsRequestValidator>();


        return services;
    }
}