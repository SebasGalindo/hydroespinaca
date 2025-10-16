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
        services.AddScoped<ISessionRepository, InMemorySessionRepository>();

        // HTTP Clients
        services.AddHttpClient<IAuthService, AuthService>();
        services.AddHttpClient<IProxyService, ProxyService>();
        services.AddHttpClient<IWeatherService, WeatherService>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProxyService, ProxyService>();
        services.AddScoped<IWeatherService, WeatherService>();

        return services;
    }
}