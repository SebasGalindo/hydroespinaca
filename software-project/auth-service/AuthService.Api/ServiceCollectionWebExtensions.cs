using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;

namespace AuthService.Web;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment? environment = null)
    {

        var jwtSettings = configuration
            .GetSection("Jwt")
            .Get<AuthService.Infrastructure.Security.JwtSettings>()
            ?? throw new InvalidOperationException("Missing Jwt section in config");


        // En entorno de test, usar KeyStore en memoria
        var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var hostEnv = environment?.EnvironmentName;
        var isTestEnvironment = aspNetCoreEnv == "Test" || hostEnv == "Test";

        Console.WriteLine($"Environment: {aspNetCoreEnv ?? hostEnv ?? "Unknown"}");

        if (isTestEnvironment)
        {
            Console.WriteLine("Using in-memory key store for testing.");
        }
        else
        {
            Console.WriteLine("Using persistent key store.");
        }

        services.AddControllers();

        RsaSecurityKey key;

        var sharedKeyStore = TestKeyStoreFactory.GetOrCreateInstance();

        var publicKeyPem = sharedKeyStore.GetPublicKey();

        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem.ToCharArray());
        key = new RsaSecurityKey(rsa);

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

                if (isTestEnvironment)
                {
                    opts.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            return Task.CompletedTask;
                        }
                    };
                }
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

        services.AddValidatorsFromAssemblyContaining<ClientCredentialsCommandValidator>();
        services.AddHealthChecks();

        return services;
    }
}
