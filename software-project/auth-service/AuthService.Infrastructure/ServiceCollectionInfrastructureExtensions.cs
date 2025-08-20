// ITokenService now in Domain.Interfaces
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
            var environment = provider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            
            // For test environment, use in-memory key store
            if (environment.EnvironmentName == "Test")
            {
                return TestKeyStoreFactory.GetOrCreateInstance();
            }

            // Get keys directory from configuration, default to "Keys" folder
            var keysDirectory = config["Jwt:KeysDirectory"] ?? "Keys";
            
            // Ensure the keys directory path is absolute
            if (!Path.IsPathRooted(keysDirectory))
            {
                keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), keysDirectory);
            }

            return new FileKeyStore(keysDirectory);
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