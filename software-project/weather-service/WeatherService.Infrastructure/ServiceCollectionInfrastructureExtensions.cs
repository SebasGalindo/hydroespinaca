using HydroEspinaca.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using WeatherService.Domain.Interfaces;
using WeatherService.Infrastructure.Clients;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Infrastructure.Workers;

namespace WeatherService.Infrastructure;

/// <summary>
/// Extension methods for registering Weather Service infrastructure dependencies.
/// </summary>
public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // In-memory cache for forecast data
        services.AddMemoryCache();

        // MongoDB
        services.AddMongoSettings(configuration);
        services.AddSingleton<IWeatherAlertConfigRepository, MongoWeatherAlertConfigRepository>();
        services.AddSingleton<IWeatherAlertRepository, MongoWeatherAlertRepository>();
        services.AddSingleton<IForecastCacheRepository, MongoForecastCacheRepository>();
        services.AddSingleton<IAlertDeliveryLogRepository, MongoAlertDeliveryLogRepository>();

        // HttpClient for OpenWeather API
        services.AddHttpClient<IOpenWeatherClient, OpenWeatherClient>();

        // HttpClient for notification-service with Polly retry (3 retries, exponential backoff)
        services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>()
            .AddPolicyHandler(GetRetryPolicy());

        // Background worker for alert evaluation
        services.AddHostedService<Workers.WeatherAlertEvaluationWorker>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));
    }
}
