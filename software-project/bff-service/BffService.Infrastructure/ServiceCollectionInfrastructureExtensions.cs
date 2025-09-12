using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BffService.Domain.Interfaces;
using BffService.Infrastructure.Cache;
using BffService.Infrastructure.Services;
using BffService.Infrastructure.Http;

namespace BffService.Infrastructure;

public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Memory cache for session storage
        services.AddMemoryCache();
        
        // Repositories
        services.AddScoped<ISessionRepository, InMemorySessionRepository>();

        // HTTP Clients
        services.AddHttpClient<IAuthService, AuthService>();
        services.AddHttpClient<IProxyService, ProxyService>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProxyService, ProxyService>();

        return services;
    }
}