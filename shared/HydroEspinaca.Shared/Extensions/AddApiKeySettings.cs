using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HydroEspinaca.Shared.Extensions;

public static class ApiKeyExtensions
{
    public static IServiceCollection AddApiKeySettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiKeySettings>()
            .Bind(configuration.GetSection("ApiKey"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
