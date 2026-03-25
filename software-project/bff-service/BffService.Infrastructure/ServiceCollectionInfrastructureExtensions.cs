using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Infrastructure.Cache;
using BffService.Infrastructure.Services;
using BffService.Infrastructure.Http;

namespace BffService.Infrastructure;

/// <summary>
/// Extension methods for registering BFF infrastructure dependencies (HTTP clients, session store, auth service).
/// </summary>
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
        services.AddHttpClient<IFuzzyServiceClient, FuzzyServiceClient>(client =>
        {
            // Clone/import operations can be slow (deep copy of variables + terms + rules)
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.AddHttpClient<IBiServiceClient, BiServiceClient>();
        services.AddHttpClient<IWeatherServiceClient, WeatherServiceClient>();
        services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>();
        services.AddHttpClient<IChatbotServiceClient, ChatbotServiceClient>(client =>
        {
            // SSE streaming can be long-lived; extend timeout to match Gemini generation
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        return services;
    }
}