using HydroEspinaca.Shared.Extensions;

namespace WeatherService.Web;

/// <summary>
/// Extension methods for registering Weather Service API layer dependencies.
/// </summary>
public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment? environment = null)
    {
        services.AddHydroEspinacaMicroservice(
            configuration,
            "weather-service",
            "Weather Service API",
            typeof(ServiceCollectionWebExtensions).Assembly
        );

        return services;
    }
}
