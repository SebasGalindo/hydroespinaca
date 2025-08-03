using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Persistence.Schemas;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Services;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;
public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Domain services
        services
            .AddScoped<IPasswordHasher, BcryptPasswordHasher>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IClientAuthenticationService, ClientAuthenticationService>()
            .AddScoped<ITokenService, JwtTokenService>()
            .AddScoped<IClientAppRegistrationService, ClientAppRegistrationService>();

        var privateKeyPath = configuration["Jwt:PrivateKeyPath"]
            ?? throw new ArgumentNullException("Jwt:PrivateKeyPath configuration is missing.");

        var publicKeyPath = configuration["Jwt:PublicKeyPath"]
            ?? throw new ArgumentNullException("Jwt:PublicKeyPath configuration is missing.");

        services.AddSingleton<IKeyStore>(new FileKeyStore(privateKeyPath, publicKeyPath));
        services.AddSingleton<IKeyStore>(new FileKeyStore(privateKeyPath, publicKeyPath));

        // Repositories
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IClientAppRepository, MongoClientAppRepository>();
        services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();

        // Mapping services
        services.AddScoped<IEntityMapper<User, UserDocument>, UserMapper>();
        services.AddScoped<IEntityMapper<ClientApp, ClientAppDocument>, ClientAppMapper>();
        services.AddScoped<IEntityMapper<RefreshToken, RefreshTokenDocument>, RefreshTokenMapper>();

        return services;
    }
}