using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Persistence.Schemas;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Services;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;
public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB configuration
        services
           .AddMongoSettings(configuration);

        // Domain services
        services
            .AddScoped<IPasswordHasher, BcryptPasswordHasher>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IClientAuthenticationService, ClientAuthenticationService>()
            .AddScoped<ITokenService, JwtTokenService>()
            .AddScoped<IClientAppRegistrationService, ClientAppRegistrationService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddSingleton<IKeyStore>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var privateKeyPath = config["Jwt:PrivateKeyPath"]
                ?? throw new ArgumentNullException("Jwt:PrivateKeyPath configuration is missing.");
            var publicKeyPath = config["Jwt:PublicKeyPath"]
                ?? throw new ArgumentNullException("Jwt:PublicKeyPath configuration is missing.");

            return new FileKeyStore(privateKeyPath, publicKeyPath);
        });

        // Repositories
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IClientAppRepository, MongoClientAppRepository>();
        services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
        services.AddScoped<IPermissionRepository, MongoPermissionRepository>();
        services.AddScoped<IRoleRepository, MongoRoleRepository>();

        // Mapping services
        services.AddScoped<IEntityMapper<User, UserDocument>, UserMapper>();
        services.AddScoped<IEntityMapper<ClientApp, ClientAppDocument>, ClientAppMapper>();
        services.AddScoped<IEntityMapper<RefreshToken, RefreshTokenDocument>, RefreshTokenMapper>();
        services.AddScoped<IEntityMapper<Permission, PermissionDocument>, PermissionMapper>();
        services.AddScoped<IEntityMapper<Role, RoleDocument>, RoleMapper>();

        // Data seeding service
        services.AddScoped<DataSeedingService>();

        return services;
    }
}