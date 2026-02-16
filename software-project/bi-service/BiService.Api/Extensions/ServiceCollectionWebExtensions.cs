using HydroEspinaca.Shared.Extensions;

namespace BiService.Api.Extensions;

public static class ServiceCollectionWebExtensions
{
    public static IServiceCollection AddBiServiceApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddHydroEspinacaMicroservice(
            configuration,
            serviceName: "bi-service",
            apiTitle: "BI Service API");

        return services;
    }
}
