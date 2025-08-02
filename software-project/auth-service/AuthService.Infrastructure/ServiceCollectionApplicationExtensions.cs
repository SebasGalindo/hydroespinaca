using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;
public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<IPasswordHasher, BcryptPasswordHasher>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IClientAuthenticationService, ClientAuthenticationService>()
            .AddScoped<ITokenService, JwtTokenService>();

        var privateKeyPath = configuration["Jwt:PrivateKeyPath"]
            ?? throw new ArgumentNullException("Jwt:PrivateKeyPath configuration is missing.");

        var publicKeyPath = configuration["Jwt:PublicKeyPath"]
            ?? throw new ArgumentNullException("Jwt:PublicKeyPath configuration is missing.");

        services.AddSingleton<IKeyStore>(new FileKeyStore(privateKeyPath, publicKeyPath));


        services.AddSingleton<IKeyStore>(new FileKeyStore(privateKeyPath, publicKeyPath));

        return services;
    }
}