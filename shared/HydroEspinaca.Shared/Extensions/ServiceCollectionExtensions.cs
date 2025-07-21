using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HydroEspinaca.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoSettings>()
            .Bind(configuration.GetSection("Mongo"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddMqttSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MqttSettings>()
            .Bind(configuration.GetSection("Mqtt"));
        return services;
    }

    public static IServiceCollection AddApiKeySettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiKeySettings>()
            .Bind(configuration.GetSection("ApiKey"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
