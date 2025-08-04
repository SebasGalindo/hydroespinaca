using AuthService.Application.Validators;
using FluentValidation;
using HydroEspinaca.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;

namespace AuthService.Web;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection("Jwt")
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing Jwt section in config");

        services.AddControllers();

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



        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });

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
        });

        services.AddValidatorsFromAssemblyContaining<ClientCredentialsRequestValidator>();


        services.AddHealthChecks();
        return services;
    }
}
