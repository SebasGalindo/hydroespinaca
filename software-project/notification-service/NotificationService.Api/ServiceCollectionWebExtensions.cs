using FluentValidation;
using HydroEspinaca.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;

namespace NotificationService.Web;

// Extensiones de la capa Web (API):
// - Controllers
// - JWT Bearer con clave pública (emitida por auth-service)
// - Swagger con esquema Bearer
// - FluentValidation (ensambla validators del proyecto)
// - HealthChecks básicos
public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

        // Controllers de ASP.NET Core
        services.AddControllers();

        // Configurar JWT solo si hay claves disponibles
        if (jwtSettings != null && !string.IsNullOrWhiteSpace(jwtSettings.PublicKeyPath) && File.Exists(jwtSettings.PublicKeyPath))
        {
            var publicKeyPem = File.ReadAllText(jwtSettings.PublicKeyPath);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
            var key = new RsaSecurityKey(rsa);

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();
        }

        services.AddEndpointsApiExplorer();
    // Swagger + esquema de seguridad Bearer
    services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });

            // Agregar esquema Bearer solo si JWT está configurado
            if (jwtSettings != null && !string.IsNullOrWhiteSpace(jwtSettings.PublicKeyPath) && File.Exists(jwtSettings.PublicKeyPath))
            {
                var scheme = new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "JWT Bearer authentication",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                c.AddSecurityDefinition("Bearer", scheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { scheme, Array.Empty<string>() }
                });
            }
        });

    // Registra validators desde este ensamblado (Application registra los suyos propios también si se requiere)
    services.AddValidatorsFromAssembly(typeof(ServiceCollectionWebExtensions).Assembly);

    // Health checks (liveness/readiness básicos). Puedes agregar checks de Mongo o proveedores luego.
    services.AddHealthChecks();
        return services;
    }
}
