using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Infrastructure.Cache;
using BffService.Infrastructure.Services;
using BffService.Infrastructure.Http;

namespace BffService.Infrastructure;

public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Memory cache for session storage and weather data
        services.AddMemoryCache();

        // Repositories
        // IMPORTANT: InMemorySessionRepository must be Singleton because:
        // 1. It's a shared cache across all requests
        // 2. It uses IMemoryCache which is already Singleton
        // 3. Creating new instances per request is wasteful and can cause issues with concurrent refresh
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

        // HTTP Clients
        services.AddHttpClient<IAuthService, AuthService>();
        services.AddHttpClient<IAuthServiceClient, AuthServiceClient>();
        services.AddHttpClient<IProxyService, ProxyService>();
        services.AddHttpClient<IWeatherService, WeatherService>();
        services.AddHttpClient<IFuzzyServiceClient, FuzzyServiceClient>();

        return services;
    }
}