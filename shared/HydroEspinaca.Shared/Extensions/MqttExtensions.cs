using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HydroEspinaca.Shared.Extensions;

public static class MqttExtensions
{
    public static IServiceCollection AddMqttSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MqttSettings>()
            .Bind(configuration.GetSection("Mqtt"));

        return services;
    }
}